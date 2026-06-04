using Microsoft.Data.SqlClient;

namespace VEMS.Services;

public sealed class ConfigurationValuesProvider : IConfigurationValuesProvider
{
    private readonly string _connectionString;

    public ConfigurationValuesProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<string>> GetValuesAsync(string configKey, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ConfigValues
            FROM dbo.Configurations
            WHERE ConfigKey = @ConfigKey AND IsActive = 1;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ConfigKey", configKey.Trim());
        await connection.OpenAsync(cancellationToken);
        var raw = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => v.Length > 0)
            .ToList();
    }

    public async Task<string> GetDefaultValueAsync(string configKey, CancellationToken cancellationToken = default)
    {
        var values = await GetValuesAsync(configKey, cancellationToken);
        return values.Count > 0 ? values[0] : string.Empty;
    }
}
