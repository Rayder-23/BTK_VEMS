using Microsoft.Data.SqlClient;
using VEMS.Areas.TeacherPortal.Models;

namespace VEMS.Areas.TeacherPortal.Services;

public sealed class ClassRepository : IClassRepository
{
    private readonly string _connectionString;

    public ClassRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ClassListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                ClassID,
                ClassCode,
                ClassName,
                SortOrder,
                IsActive
            FROM dbo.Classes
            WHERE (@Search IS NULL
                   OR ClassName LIKE @Search
                   OR ClassCode LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + """
             ORDER BY SortOrder, ClassName;
            """;

        var list = new List<ClassListItem>();
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

    public async Task<ClassFormModel?> GetAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ClassID,
                ClassCode,
                ClassName,
                SortOrder,
                IsActive
            FROM dbo.Classes
            WHERE ClassID = @ClassId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassId", classId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<bool> ClassCodeExistsAsync(string classCode, int? excludeClassId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(classCode))
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Classes
            WHERE ClassCode = @ClassCode
              AND (@ExcludeClassId IS NULL OR ClassID <> @ExcludeClassId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassCode", classCode.Trim());
        command.Parameters.AddWithValue("@ExcludeClassId", (object?)excludeClassId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ClassFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Classes (
                ClassCode,
                ClassName,
                SortOrder,
                IsActive
            )
            VALUES (
                @ClassCode,
                @ClassName,
                @SortOrder,
                @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ClassFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Classes SET
                ClassCode = @ClassCode,
                ClassName = @ClassName,
                SortOrder = @SortOrder,
                IsActive = @IsActive
            WHERE ClassID = @ClassId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassId", model.ClassId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetActiveAsync(int classId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Classes SET IsActive = @IsActive
            WHERE ClassID = @ClassId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassId", classId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int classId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Classes WHERE ClassID = @ClassId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassId", classId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ClassListItem MapListItem(SqlDataReader reader) => new()
    {
        ClassId = Convert.ToInt32(reader["ClassID"]),
        ClassCode = reader["ClassCode"] as string ?? string.Empty,
        ClassName = reader["ClassName"] as string ?? string.Empty,
        SortOrder = reader["SortOrder"] is DBNull ? null : Convert.ToInt32(reader["SortOrder"]),
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static ClassFormModel MapForm(SqlDataReader reader) => new()
    {
        ClassId = Convert.ToInt32(reader["ClassID"]),
        ClassCode = reader["ClassCode"] as string,
        ClassName = reader["ClassName"] as string ?? string.Empty,
        SortOrder = reader["SortOrder"] is DBNull ? null : Convert.ToInt32(reader["SortOrder"]),
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static void Bind(SqlCommand command, ClassFormModel model)
    {
        command.Parameters.AddWithValue("@ClassCode", string.IsNullOrWhiteSpace(model.ClassCode) ? DBNull.Value : model.ClassCode.Trim());
        command.Parameters.AddWithValue("@ClassName", model.ClassName.Trim());
        command.Parameters.AddWithValue("@SortOrder", (object?)model.SortOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
