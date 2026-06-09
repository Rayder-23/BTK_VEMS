using Microsoft.Data.SqlClient;

namespace VEMS.Areas.TeacherPortal.Services;

public sealed class TeacherAccountRepository : ITeacherAccountRepository
{
    private readonly string _connectionString;

    public TeacherAccountRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<string?> GetPasswordHashByLoginUidAsync(int loginUid, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PasswordHash FROM dbo.EmployeeLogin WHERE Uid = @LoginUid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LoginUid", loginUid);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : result.ToString();
    }

    public async Task<bool> UpdatePasswordAsync(int loginUid, string passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.EmployeeLogin
            SET PasswordHash = @PasswordHash
            WHERE Uid = @LoginUid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LoginUid", loginUid);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}
