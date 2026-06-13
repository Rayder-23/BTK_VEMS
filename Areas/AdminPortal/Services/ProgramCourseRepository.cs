using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ProgramCourseRepository : IProgramCourseRepository
{
    private readonly string _connectionString;

    public ProgramCourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ProgramCourseListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                pc.UID,
                p.ProgramName,
                c.CourseCode,
                c.CourseName
            FROM dbo.ProgramCourses pc
            INNER JOIN dbo.Programs p ON pc.ProgramID = p.ProgramID
            INNER JOIN dbo.Courses c ON pc.CourseID = c.CourseID
            WHERE (@Search IS NULL
                   OR p.ProgramName LIKE @Search
                   OR p.ProgramCode LIKE @Search
                   OR c.CourseName LIKE @Search
                   OR c.CourseCode LIKE @Search)
            ORDER BY p.ProgramName, c.CourseCode, c.CourseName;
            """;

        var list = new List<ProgramCourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ProgramCourseListItem
            {
                ProgramCourseId = Convert.ToInt32(reader["UID"]),
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<ProgramCourseFormModel?> GetAsync(int programCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, ProgramID, CourseID
            FROM dbo.ProgramCourses
            WHERE UID = @ProgramCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCourseId", programCourseId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapForm(reader);
    }

    public async Task<ProgramCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string programsSql = """
            SELECT ProgramID, ProgramCode + ' · ' + ProgramName
            FROM dbo.Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + ' · ', '') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode, CourseName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new ProgramCourseLookups
        {
            Programs = await ReadLookupAsync(connection, programsSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int programId,
        int courseId,
        int? excludeProgramCourseId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ProgramCourses
            WHERE ProgramID = @ProgramId
              AND CourseID = @CourseId
              AND (@ExcludeProgramCourseId IS NULL OR UID <> @ExcludeProgramCourseId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@ExcludeProgramCourseId", (object?)excludeProgramCourseId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ProgramCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ProgramCourses (ProgramID, CourseID)
            VALUES (@ProgramId, @CourseId);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ProgramCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ProgramCourses SET
                ProgramID = @ProgramId,
                CourseID = @CourseId
            WHERE UID = @ProgramCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCourseId", model.ProgramCourseId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int programCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.ProgramCourses WHERE UID = @ProgramCourseId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCourseId", programCourseId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, ProgramCourseFormModel model)
    {
        command.Parameters.AddWithValue("@ProgramId", model.ProgramId);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
    }

    private static ProgramCourseFormModel MapForm(SqlDataReader reader) => new()
    {
        ProgramCourseId = Convert.ToInt32(reader["UID"]),
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        CourseId = Convert.ToInt32(reader["CourseID"])
    };

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentLookupItem
            {
                Id = Convert.ToInt32(reader[0]),
                Name = reader[1] as string ?? string.Empty
            });
        }

        return list;
    }
}
