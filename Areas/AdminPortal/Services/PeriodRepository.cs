using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class PeriodRepository : IPeriodRepository
{
    private readonly string _connectionString;

    public PeriodRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<PeriodListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT PeriodID, PeriodName, StartTime, EndTime
            FROM dbo.Periods
            WHERE (@Search IS NULL
                   OR PeriodName LIKE @Search)
            ORDER BY StartTime, PeriodName;
            """;

        var list = new List<PeriodListItem>();
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

    public async Task<PeriodFormModel?> GetAsync(int periodId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT PeriodID, PeriodName, StartTime, EndTime
            FROM dbo.Periods
            WHERE PeriodID = @PeriodId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PeriodId", periodId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<int> InsertAsync(PeriodFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Periods (PeriodName, StartTime, EndTime)
            VALUES (@PeriodName, @StartTime, @EndTime);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(PeriodFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Periods SET
                PeriodName = @PeriodName,
                StartTime = @StartTime,
                EndTime = @EndTime
            WHERE PeriodID = @PeriodId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PeriodId", model.PeriodId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int periodId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Periods WHERE PeriodID = @PeriodId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PeriodId", periodId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, PeriodFormModel model)
    {
        command.Parameters.AddWithValue("@PeriodName", string.IsNullOrWhiteSpace(model.PeriodName) ? DBNull.Value : model.PeriodName.Trim());
        command.Parameters.AddWithValue("@StartTime", model.StartTime.HasValue ? model.StartTime.Value : DBNull.Value);
        command.Parameters.AddWithValue("@EndTime", model.EndTime.HasValue ? model.EndTime.Value : DBNull.Value);
    }

    private static PeriodListItem MapListItem(SqlDataReader reader) => new()
    {
        PeriodId = Convert.ToInt32(reader["PeriodID"]),
        PeriodName = reader["PeriodName"] as string,
        StartTime = reader["StartTime"] is DBNull ? null : (TimeSpan)reader["StartTime"],
        EndTime = reader["EndTime"] is DBNull ? null : (TimeSpan)reader["EndTime"]
    };

    private static PeriodFormModel MapForm(SqlDataReader reader) => new()
    {
        PeriodId = Convert.ToInt32(reader["PeriodID"]),
        PeriodName = reader["PeriodName"] as string,
        StartTime = reader["StartTime"] is DBNull ? null : (TimeSpan)reader["StartTime"],
        EndTime = reader["EndTime"] is DBNull ? null : (TimeSpan)reader["EndTime"]
    };
}
