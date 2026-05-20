using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ConfigurationsRepository : IConfigurationsRepository
{
    private const int ValuePreviewMaxLength = 80;
    private readonly string _connectionString;

    public ConfigurationsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ConfigurationListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ConfigKey,
                ConfigValues,
                Description,
                IsActive,
                UpdatedAt
            FROM dbo.Configurations
            ORDER BY ConfigKey ASC;
            """;

        var list = new List<ConfigurationListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fullValue = reader["ConfigValues"] as string ?? string.Empty;
            list.Add(new ConfigurationListItemViewModel
            {
                Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
                ConfigKey = reader["ConfigKey"] as string ?? string.Empty,
                ConfigValuesPreview = Preview(fullValue),
                Description = reader["Description"] as string,
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            });
        }

        return list;
    }

    public async Task<ConfigurationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ConfigKey,
                ConfigValues,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM dbo.Configurations
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ConfigurationFormModel
        {
            Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
            ConfigKey = reader["ConfigKey"] as string ?? string.Empty,
            ConfigValues = reader["ConfigValues"] as string ?? string.Empty,
            Description = reader["Description"] as string,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    public async Task<int> InsertAsync(ConfigurationFormModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        const string sql = """
            INSERT INTO dbo.Configurations (
                ConfigKey,
                ConfigValues,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT INSERTED.Uid
            VALUES (
                @ConfigKey,
                @ConfigValues,
                @Description,
                @IsActive,
                @CreatedAt,
                @UpdatedAt
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddWriteParameters(command, model, now, setTimestamps: true);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(ConfigurationFormModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        const string sql = """
            UPDATE dbo.Configurations
            SET
                ConfigKey = @ConfigKey,
                ConfigValues = @ConfigValues,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        AddWriteParameters(command, model, now, setTimestamps: false);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> UpdateIsActiveAsync(int uid, bool isActive, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        const string sql = """
            UPDATE dbo.Configurations
            SET
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@IsActive", isActive);
        command.Parameters.AddWithValue("@UpdatedAt", now);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Configurations WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static void AddWriteParameters(SqlCommand command, ConfigurationFormModel model, DateTime now, bool setTimestamps)
    {
        command.Parameters.AddWithValue("@ConfigKey", model.ConfigKey.Trim());
        command.Parameters.AddWithValue("@ConfigValues", model.ConfigValues);
        command.Parameters.AddWithValue("@Description", (object?)model.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        if (setTimestamps)
        {
            command.Parameters.AddWithValue("@CreatedAt", now);
            command.Parameters.AddWithValue("@UpdatedAt", now);
        }
        else
        {
            command.Parameters.AddWithValue("@UpdatedAt", now);
        }
    }

    private static string Preview(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var oneLine = value.ReplaceLineEndings(" ");
        return oneLine.Length <= ValuePreviewMaxLength
            ? oneLine
            : string.Concat(oneLine.AsSpan(0, ValuePreviewMaxLength), "…");
    }
}
