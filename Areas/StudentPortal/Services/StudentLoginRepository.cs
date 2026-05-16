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
        var trimmedUsername = username.Trim();
        int? studentId = int.TryParse(trimmedUsername, out var parsedStudentId) ? parsedStudentId : null;

        const string sql = """
            SELECT TOP (1)
                Uid,
                StudentId,
                Username,
                PasswordHash
            FROM dbo.StudentsLogin
            WHERE Username = @username
               OR (@studentId IS NOT NULL AND StudentId = @studentId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@username", trimmedUsername);
        command.Parameters.AddWithValue("@studentId", (object?)studentId ?? DBNull.Value);

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
            StudentId = reader["StudentId"] is DBNull ? null : reader["StudentId"].ToString(),
            Username = reader["Username"] as string ?? trimmedUsername,
            Role = "Student" // Hardcoded since we know this is the Student portal
        };
    }
}
