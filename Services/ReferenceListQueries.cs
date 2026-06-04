using Microsoft.Data.SqlClient;

namespace VEMS.Services;

internal static class ReferenceListQueries
{
    public static async Task<IReadOnlyList<string>> GetActiveCountryNamesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CountryName
            FROM dbo.ref_Countries
            WHERE IsActive = 1
            ORDER BY CountryName;
            """;

        var list = await ReadNameColumnAsync(connectionString, sql, "CountryName", cancellationToken);
        if (!list.Any(c => string.Equals(c, "Pakistan", StringComparison.OrdinalIgnoreCase)))
        {
            return new[] { "Pakistan" }.Concat(list).ToList();
        }

        return list;
    }

    public static async Task<IReadOnlyList<string>> GetActiveProvinceNamesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ProvinceName
            FROM dbo.ref_Provinces
            WHERE IsActive = 1
            ORDER BY ProvinceName;
            """;

        return await ReadNameColumnAsync(connectionString, sql, "ProvinceName", cancellationToken);
    }

    public static async Task<IReadOnlyList<string>> GetActiveCityNamesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CityName
            FROM dbo.ref_Cities
            WHERE IsActive = 1
            ORDER BY CityName;
            """;

        return await ReadNameColumnAsync(connectionString, sql, "CityName", cancellationToken);
    }

    private static async Task<IReadOnlyList<string>> ReadNameColumnAsync(
        string connectionString,
        string sql,
        string column,
        CancellationToken cancellationToken)
    {
        var list = new List<string>();
        await using var connection = new SqlConnection(connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader[column] as string;
            if (!string.IsNullOrWhiteSpace(name))
            {
                list.Add(name.Trim());
            }
        }

        return list;
    }
}
