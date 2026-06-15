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
                s.CreatedOn,
                se.ProgramName,
                se.ProgramCode,
                se.YearName,
                se.ClassSectionDisplay,
                se.RollNo,
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
                    p.ProgramCode,
                    ay.YearName,
                    CASE
                        WHEN cs.ClassSectionID IS NULL THEN NULL
                        ELSE c.ClassName + N' · ' + sec.SectionName
                    END AS ClassSectionDisplay,
                    e.RollNo,
                    e.EnrollmentDate
                FROM dbo.StudentEnrollments e
                INNER JOIN dbo.Programs p ON e.ProgramID = p.ProgramID
                INNER JOIN dbo.AcademicYears ay ON e.AcademicYearID = ay.AcademicYearID
                LEFT JOIN dbo.ClassSections cs ON e.ClassSectionID = cs.ClassSectionID
                LEFT JOIN dbo.Classes c ON cs.ClassID = c.ClassID
                LEFT JOIN dbo.Sections sec ON cs.SectionID = sec.SectionID
                WHERE e.StudentID = s.StudentID
                ORDER BY e.EnrollmentDate DESC, ay.YearName DESC
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

        return new StudentProfileViewModel
        {
            StudentId = ToInt32(reader, "StudentID"),
            RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
            FullName = reader["StudentName"] as string ?? string.Empty,
            MobileNo = reader["MobileNo"] as string,
            Email = reader["Email"] as string,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
            ProgramName = reader["ProgramName"] as string,
            ProgramCode = reader["ProgramCode"] as string,
            AcademicYearName = reader["YearName"] as string,
            ClassSectionDisplay = reader["ClassSectionDisplay"] as string,
            RollNo = reader["RollNo"] is DBNull ? null : ToInt32(reader, "RollNo"),
            EnrollmentDate = reader["EnrollmentDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("EnrollmentDate")),
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
