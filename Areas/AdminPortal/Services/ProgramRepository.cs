using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ProgramRepository : IProgramRepository
{
    private readonly string _connectionString;

    public ProgramRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ProgramListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                ProgramID,
                ProgramCode,
                ProgramName,
                DurationYears,
                IsActive,
                CreatedOn
            FROM dbo.Programs
            WHERE (@Search IS NULL
                   OR ProgramCode LIKE @Search
                   OR ProgramName LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + """
             ORDER BY ProgramCode;
            """;

        var list = new List<ProgramListItem>();
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

    public async Task<ProgramListItem?> GetListItemAsync(int programId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ProgramID,
                ProgramCode,
                ProgramName,
                DurationYears,
                IsActive,
                CreatedOn
            FROM dbo.Programs
            WHERE ProgramID = @ProgramId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapListItem(reader) : null;
    }

    public async Task<IReadOnlyList<StudentLookupItem>> GetProgramOptionsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT ProgramID, ProgramCode + ' - ' + ProgramName
            FROM dbo.Programs
            """ + (activeOnly ? " WHERE IsActive = 1" : "") + """
             ORDER BY ProgramName;
            """;

        var list = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
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

    public async Task<ProgramFormModel?> GetAsync(int programId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ProgramID,
                ProgramCode,
                ProgramName,
                DurationYears,
                IsActive,
                CreatedOn
            FROM dbo.Programs
            WHERE ProgramID = @ProgramId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<bool> ProgramCodeExistsAsync(string programCode, int? excludeProgramId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Programs
            WHERE ProgramCode = @ProgramCode
              AND (@ExcludeProgramId IS NULL OR ProgramID <> @ExcludeProgramId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCode", programCode.Trim());
        command.Parameters.AddWithValue("@ExcludeProgramId", (object?)excludeProgramId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ProgramFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Programs (
                ProgramCode,
                ProgramName,
                DurationYears,
                IsActive
            )
            VALUES (
                @ProgramCode,
                @ProgramName,
                @DurationYears,
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

    public async Task<bool> UpdateAsync(ProgramFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Programs SET
                ProgramCode = @ProgramCode,
                ProgramName = @ProgramName,
                DurationYears = @DurationYears,
                IsActive = @IsActive
            WHERE ProgramID = @ProgramId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", model.ProgramId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetActiveAsync(int programId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Programs SET IsActive = @IsActive
            WHERE ProgramID = @ProgramId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int programId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Programs WHERE ProgramID = @ProgramId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ProgramListItem MapListItem(SqlDataReader reader) => new()
    {
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToInt32(reader["DurationYears"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
    };

    private static ProgramFormModel MapForm(SqlDataReader reader) => new()
    {
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToInt32(reader["DurationYears"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
    };

    private static void Bind(SqlCommand command, ProgramFormModel model)
    {
        command.Parameters.AddWithValue("@ProgramCode", model.ProgramCode.Trim());
        command.Parameters.AddWithValue("@ProgramName", model.ProgramName.Trim());
        command.Parameters.AddWithValue("@DurationYears", (object?)model.DurationYears ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
