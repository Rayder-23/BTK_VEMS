using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentProfileRepository : IStudentProfileRepository
{
    private readonly string _connectionString;

    public StudentProfileRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<StudentProfileViewModel?> GetByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.StudentID,
                s.RegistrationNo,
                s.StudentName,
                s.MobileNo,
                s.Email,
                s.IsActive,
                se.ProgramName,
                se.RollNo,
                se.AcademicYear,
                se.EnrollmentDate,
                sl.Username,
                sl.Email AS LoginEmail,
                sl.Status AS LoginStatus,
                sl.LastLoginAt
            FROM dbo.Students s
            LEFT JOIN dbo.StudentsLogin sl ON sl.StudentId = s.StudentID
            OUTER APPLY (
                SELECT TOP 1
                    p.ProgramName,
                    e.RollNo,
                    e.AcademicYear,
                    e.EnrollmentDate
                FROM dbo.StudentEnrollments e
                INNER JOIN dbo.Programs p ON e.ProgramID = p.ProgramID
                WHERE e.StudentID = s.StudentID AND e.IsActive = 1
                ORDER BY e.AcademicYear DESC, e.GradeOrSemester DESC
            ) se
            WHERE s.StudentID = @StudentUid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var enrollmentDate = reader["EnrollmentDate"] is DBNull
            ? DateTime.UtcNow.Date
            : reader.GetDateTime(reader.GetOrdinal("EnrollmentDate"));

        return new StudentProfileViewModel
        {
            Uid = ToInt32(reader, "StudentID"),
            RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
            RollNo = reader["RollNo"] as string,
            FullName = reader["StudentName"] as string ?? string.Empty,
            FatherName = string.Empty,
            DateOfBirth = DateTime.MinValue,
            Gender = string.Empty,
            AdmissionYear = reader["AcademicYear"] is DBNull ? (short)0 : reader.GetInt16(reader.GetOrdinal("AcademicYear")),
            AdmissionDate = enrollmentDate,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            ProgramName = reader["ProgramName"] as string,
            PortalUsername = reader["Username"] as string,
            PortalEmail = reader["LoginEmail"] as string ?? reader["Email"] as string,
            PortalStatus = reader["LoginStatus"] as string,
            LastLoginAt = reader["LastLoginAt"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt"))
        };
    }

    public async Task<int?> ResolveStudentUidByLoginUidAsync(int loginUid, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT StudentId FROM dbo.StudentsLogin WHERE Uid = @LoginUid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LoginUid", loginUid);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    public async Task<string?> GetPasswordHashByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PasswordHash FROM dbo.StudentsLogin WHERE StudentId = @StudentUid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : result.ToString();
    }

    public async Task<bool> UpdatePasswordAsync(int studentUid, string passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentsLogin
            SET PasswordHash = @PasswordHash,
                PasswordChangedAt = SYSUTCDATETIME(),
                MustChangePassword = 0
            WHERE StudentId = @StudentUid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static int ToInt32(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));
}
