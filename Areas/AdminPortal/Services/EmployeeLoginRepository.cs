using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.StudentPortal.Services;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class EmployeeLoginRepository : IEmployeeLoginRepository
{
    private const string RolesConfigKey = "EmployeeRoles";
    private const string StatusConfigKey = "EmployeeStatus";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public EmployeeLoginRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<EmployeeLoginListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                el.Uid,
                el.EmployeeId,
                el.Username,
                el.Role,
                el.Status,
                el.CreatedOn,
                e.EmployeeId AS EmployeeCode,
                e.FullName
            FROM dbo.EmployeeLogin el
            INNER JOIN dbo.Employee e ON el.EmployeeId = e.Uid
            ORDER BY el.Uid DESC;
            """;

        var list = new List<EmployeeLoginListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new EmployeeLoginListItemViewModel
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                EmployeeUid = Convert.ToInt32(reader["EmployeeId"]),
                Username = reader["Username"] as string ?? string.Empty,
                Role = reader["Role"] as string ?? string.Empty,
                Status = reader["Status"] as string ?? string.Empty,
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                EmployeeCode = reader["EmployeeCode"] as string ?? string.Empty,
                EmployeeName = reader["FullName"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<EmployeeLoginFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                el.Uid,
                el.EmployeeId,
                el.Username,
                el.Role,
                el.Status,
                e.EmployeeId AS EmployeeCode,
                e.FullName
            FROM dbo.EmployeeLogin el
            INNER JOIN dbo.Employee e ON el.EmployeeId = e.Uid
            WHERE el.Uid = @Uid;
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

        return MapForm(reader);
    }

    public async Task<EmployeeLoginLookups> GetLookupsAsync(CancellationToken cancellationToken = default) =>
        new()
        {
            Roles = await _configurations.GetValuesAsync(RolesConfigKey, cancellationToken),
            Statuses = await _configurations.GetValuesAsync(StatusConfigKey, cancellationToken)
        };

    public async Task<IReadOnlyList<StudentLookupItem>> GetEmployeesWithoutLoginAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                e.Uid,
                e.EmployeeId + ' — ' + e.FullName
            FROM dbo.Employee e
            WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeLogin el WHERE el.EmployeeId = e.Uid)
            ORDER BY e.EmployeeId;
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
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUid = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.EmployeeLogin
            WHERE Username = @Username
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", username.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(EmployeeLoginFormModel model, string plainPassword, int? createdBy, CancellationToken cancellationToken = default)
    {
        var hash = StudentPasswordHasher.HashPassword(plainPassword);

        const string sql = """
            INSERT INTO dbo.EmployeeLogin (
                EmployeeId,
                Username,
                PasswordHash,
                Role,
                Status,
                CreatedBy,
                CreatedOn
            )
            VALUES (
                @EmployeeId,
                @Username,
                @PasswordHash,
                @Role,
                @Status,
                @CreatedBy,
                SYSUTCDATETIME()
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        BindWrite(command, model, hash, createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(EmployeeLoginFormModel model, string? plainPassword, CancellationToken cancellationToken = default)
    {
        var updatePassword = !string.IsNullOrWhiteSpace(plainPassword);
        var hash = updatePassword ? StudentPasswordHasher.HashPassword(plainPassword!) : string.Empty;

        const string sql = """
            UPDATE dbo.EmployeeLogin SET
                Username = @Username,
                Role = @Role,
                Status = @Status,
                PasswordHash = CASE WHEN @UpdatePassword = 1 THEN @PasswordHash ELSE PasswordHash END
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        command.Parameters.AddWithValue("@Username", model.Username.Trim());
        command.Parameters.AddWithValue("@Role", model.Role.Trim());
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@UpdatePassword", updatePassword);
        command.Parameters.AddWithValue("@PasswordHash", hash);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void BindWrite(SqlCommand command, EmployeeLoginFormModel model, string passwordHash, int? createdBy)
    {
        command.Parameters.AddWithValue("@EmployeeId", model.EmployeeUid);
        command.Parameters.AddWithValue("@Username", model.Username.Trim());
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@Role", model.Role.Trim());
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
    }

    private static EmployeeLoginFormModel MapForm(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        EmployeeUid = Convert.ToInt32(reader["EmployeeId"]),
        Username = reader["Username"] as string ?? string.Empty,
        Role = reader["Role"] as string ?? string.Empty,
        Status = reader["Status"] as string ?? string.Empty,
        EmployeeCode = reader["EmployeeCode"] as string,
        EmployeeDisplayName = reader["FullName"] as string
    };
}
