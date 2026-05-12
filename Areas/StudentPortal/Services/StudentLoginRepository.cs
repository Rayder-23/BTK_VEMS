using Microsoft.Data.SqlClient;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentLoginRepository : IStudentLoginRepository
{
    private readonly string _connectionString;

    public StudentLoginRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<StudentLoginUser?> ValidateCredentialsAsync(string username, string password)
    {
        // 1. Updated SQL: Changed uid to Uid, removed Role column
        const string sql = """
            SELECT TOP (1)
                Uid,
                StudentId,
                Username,
                PasswordHash
            FROM dbo.StudentsLogin
            WHERE Username = @username OR StudentId = @username;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username.Trim());

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var storedHash = reader["PasswordHash"] as string ?? string.Empty;
        if (!StudentPasswordHasher.VerifyPassword(password, storedHash))
        {
            return null;
        }

        // 2. Updated Mapping: Matches new CamelCase Uid and hardcodes "Student"
        return new StudentLoginUser
        {
            Uid = Convert.ToInt32(reader["Uid"]), // Use CamelCase
            StudentId = reader["StudentId"] as string,
            Username = reader["Username"] as string ?? username,
            Role = "Student" // Hardcoded since we know this is the Student portal
        };
    }
}
