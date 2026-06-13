using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class SectionRepository : ISectionRepository
{
    private readonly string _connectionString;

    public SectionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<SectionListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                SectionID,
                SectionName,
                IsActive
            FROM dbo.Sections
            WHERE (@Search IS NULL
                   OR SectionName LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + """
             ORDER BY SectionName;
            """;

        var list = new List<SectionListItem>();
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

    public async Task<SectionFormModel?> GetAsync(int sectionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                SectionID,
                SectionName,
                IsActive
            FROM dbo.Sections
            WHERE SectionID = @SectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SectionId", sectionId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<bool> NameExistsAsync(
        string sectionName,
        int? excludeSectionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Sections
            WHERE SectionName = @SectionName
              AND (@ExcludeSectionId IS NULL OR SectionID <> @ExcludeSectionId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SectionName", sectionName.Trim());
        command.Parameters.AddWithValue("@ExcludeSectionId", (object?)excludeSectionId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(SectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Sections (SectionName, IsActive)
            VALUES (@SectionName, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(SectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Sections SET
                SectionName = @SectionName,
                IsActive = @IsActive
            WHERE SectionID = @SectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SectionId", model.SectionId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetActiveAsync(int sectionId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Sections
            SET IsActive = @IsActive
            WHERE SectionID = @SectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SectionId", sectionId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int sectionId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Sections WHERE SectionID = @SectionId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SectionId", sectionId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, SectionFormModel model)
    {
        command.Parameters.AddWithValue("@SectionName", model.SectionName.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }

    private static SectionListItem MapListItem(SqlDataReader reader) => new()
    {
        SectionId = Convert.ToInt32(reader["SectionID"]),
        SectionName = reader["SectionName"] as string ?? string.Empty,
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static SectionFormModel MapForm(SqlDataReader reader) => new()
    {
        SectionId = Convert.ToInt32(reader["SectionID"]),
        SectionName = reader["SectionName"] as string ?? string.Empty,
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };
}
