using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class CourseRepository : ICourseRepository
{
    private readonly string _connectionString;

    public CourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<CourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                CourseID,
                CourseCode,
                CourseName,
                CreditHours,
                IsActive
            FROM dbo.Courses
            WHERE (@Search IS NULL
                   OR CourseCode LIKE @Search
                   OR CourseName LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + """
             ORDER BY CourseCode, CourseName;
            """;

        var list = new List<CourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapListItem(reader));
        }

        return list;
    }

    public async Task<CourseFormModel?> GetAsync(int courseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                CourseID,
                CourseCode,
                CourseName,
                CreditHours,
                IsActive
            FROM dbo.Courses
            WHERE CourseID = @CourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseId", courseId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<bool> CourseCodeExistsAsync(string courseCode, int? excludeCourseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Courses
            WHERE CourseCode = @CourseCode
              AND (@ExcludeCourseId IS NULL OR CourseID <> @ExcludeCourseId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseCode", courseCode.Trim());
        command.Parameters.AddWithValue("@ExcludeCourseId", (object?)excludeCourseId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(CourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Courses (CourseCode, CourseName, CreditHours, IsActive)
            VALUES (@CourseCode, @CourseName, @CreditHours, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(CourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Courses SET
                CourseCode = @CourseCode,
                CourseName = @CourseName,
                CreditHours = @CreditHours,
                IsActive = @IsActive
            WHERE CourseID = @CourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int courseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Courses SET IsActive = 0
            WHERE CourseID = @CourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseId", courseId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static CourseListItem MapListItem(SqlDataReader reader) => new()
    {
        CourseId = Convert.ToInt32(reader["CourseID"]),
        CourseCode = reader["CourseCode"] as string ?? string.Empty,
        CourseName = reader["CourseName"] as string ?? string.Empty,
        CreditHours = reader["CreditHours"] is DBNull ? null : Convert.ToInt32(reader["CreditHours"]),
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static CourseFormModel MapForm(SqlDataReader reader) => new()
    {
        CourseId = Convert.ToInt32(reader["CourseID"]),
        CourseCode = reader["CourseCode"] as string,
        CourseName = reader["CourseName"] as string ?? string.Empty,
        CreditHours = reader["CreditHours"] is DBNull ? null : Convert.ToInt32(reader["CreditHours"]),
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static void Bind(SqlCommand command, CourseFormModel model)
    {
        command.Parameters.AddWithValue("@CourseCode", string.IsNullOrWhiteSpace(model.CourseCode) ? DBNull.Value : model.CourseCode.Trim());
        command.Parameters.AddWithValue("@CourseName", model.CourseName.Trim());
        command.Parameters.AddWithValue("@CreditHours", (object?)model.CreditHours ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
