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
                Uid,
                ProgramCode,
                ProgramName,
                ShortName,
                DurationYears,
                IsActive,
                CreatedAt
            FROM dbo.ref_Programs
            WHERE (@Search IS NULL
                   OR ProgramCode LIKE @Search
                   OR ProgramName LIKE @Search
                   OR ShortName LIKE @Search)
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

    public async Task<ProgramListItem?> GetListItemAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ProgramCode,
                ProgramName,
                ShortName,
                DurationYears,
                IsActive,
                CreatedAt
            FROM dbo.ref_Programs
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapListItem(reader) : null;
    }

    public async Task<IReadOnlyList<StudentLookupItem>> GetProgramOptionsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
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

    public async Task<ProgramFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ProgramCode,
                ProgramName,
                ShortName,
                DurationYears,
                IsActive,
                CreatedAt
            FROM dbo.ref_Programs
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<bool> ProgramCodeExistsAsync(string programCode, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ref_Programs
            WHERE ProgramCode = @ProgramCode
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCode", programCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ProgramFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ref_Programs (
                ProgramCode,
                ProgramName,
                ShortName,
                DurationYears,
                IsActive
            )
            VALUES (
                @ProgramCode,
                @ProgramName,
                @ShortName,
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
            UPDATE dbo.ref_Programs SET
                ProgramCode = @ProgramCode,
                ProgramName = @ProgramName,
                ShortName = @ShortName,
                DurationYears = @DurationYears,
                IsActive = @IsActive
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ref_Programs SET IsActive = 0
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ProgramListItem MapListItem(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToByte(reader["DurationYears"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static ProgramFormModel MapForm(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToByte(reader["DurationYears"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static void Bind(SqlCommand command, ProgramFormModel model)
    {
        command.Parameters.AddWithValue("@ProgramCode", model.ProgramCode.Trim());
        command.Parameters.AddWithValue("@ProgramName", model.ProgramName.Trim());
        command.Parameters.AddWithValue("@ShortName", (object?)model.ShortName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@DurationYears", (object?)model.DurationYears ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
