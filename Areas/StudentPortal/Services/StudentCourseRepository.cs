using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentCourseRepository : IStudentCourseRepository
{
    private readonly string _connectionString;

    public StudentCourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<StudentAllCoursesPageModel> GetAssignedCoursesAsync(
        int studentUid,
        CancellationToken cancellationToken = default)
    {
        const string enrollmentSql = """
            SELECT
                se.UID,
                se.ProgramID,
                p.ProgramName,
                p.ProgramCode,
                ay.YearName,
                CASE
                    WHEN cs.ClassSectionID IS NULL THEN NULL
                    ELSE c.ClassName + N' · ' + sec.SectionName
                END AS ClassSectionDisplay,
                se.RollNo,
                se.EnrollmentDate
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Programs p ON p.ProgramID = se.ProgramID
            INNER JOIN dbo.AcademicYears ay ON ay.AcademicYearID = se.AcademicYearID
            LEFT JOIN dbo.ClassSections cs ON se.ClassSectionID = cs.ClassSectionID
            LEFT JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            LEFT JOIN dbo.Sections sec ON cs.SectionID = sec.SectionID
            WHERE se.StudentID = @StudentUid
            ORDER BY se.EnrollmentDate DESC, ay.YearName DESC;
            """;

        const string coursesSql = """
            SELECT
                se.UID AS EnrollmentId,
                co.CourseID,
                co.CourseCode,
                co.CourseName,
                co.CreditHours,
                co.IsActive,
                p.ProgramName,
                p.ProgramCode,
                ay.YearName,
                CASE
                    WHEN cs.ClassSectionID IS NULL THEN NULL
                    ELSE c.ClassName + N' · ' + sec.SectionName
                END AS ClassSectionDisplay
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Programs p ON p.ProgramID = se.ProgramID
            INNER JOIN dbo.AcademicYears ay ON ay.AcademicYearID = se.AcademicYearID
            INNER JOIN dbo.ProgramCourses pc ON pc.ProgramID = se.ProgramID
            INNER JOIN dbo.Courses co ON co.CourseID = pc.CourseID AND co.IsActive = 1
            LEFT JOIN dbo.ClassSections cs ON se.ClassSectionID = cs.ClassSectionID
            LEFT JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            LEFT JOIN dbo.Sections sec ON cs.SectionID = sec.SectionID
            WHERE se.StudentID = @StudentUid
            ORDER BY ay.YearName DESC, p.ProgramName, co.CourseCode;
            """;

        var enrollments = new List<StudentEnrollmentContext>();
        var courses = new List<StudentAssignedCourseItem>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(enrollmentSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                enrollments.Add(new StudentEnrollmentContext
                {
                    EnrollmentId = Convert.ToInt32(reader["UID"]),
                    ProgramId = Convert.ToInt32(reader["ProgramID"]),
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
                    AcademicYearName = reader["YearName"] as string ?? string.Empty,
                    ClassSectionDisplay = reader["ClassSectionDisplay"] as string,
                    RollNo = reader["RollNo"] is DBNull ? null : Convert.ToInt32(reader["RollNo"]),
                    EnrollmentDate = reader.GetDateTime(reader.GetOrdinal("EnrollmentDate"))
                });
            }
        }

        await using (var command = new SqlCommand(coursesSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                courses.Add(new StudentAssignedCourseItem
                {
                    EnrollmentId = Convert.ToInt32(reader["EnrollmentId"]),
                    CourseId = Convert.ToInt32(reader["CourseID"]),
                    CourseCode = reader["CourseCode"] as string ?? string.Empty,
                    CourseName = reader["CourseName"] as string ?? string.Empty,
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
                    CreditHours = reader["CreditHours"] is DBNull ? null : Convert.ToInt32(reader["CreditHours"]),
                    AcademicYearName = reader["YearName"] as string ?? string.Empty,
                    ClassSectionDisplay = reader["ClassSectionDisplay"] as string,
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
        }

        var primaryEnrollment = enrollments.FirstOrDefault();

        return new StudentAllCoursesPageModel
        {
            ProgramName = primaryEnrollment?.ProgramName,
            ProgramCode = primaryEnrollment?.ProgramCode,
            RollNo = primaryEnrollment?.RollNo,
            AcademicYearName = primaryEnrollment?.AcademicYearName,
            Enrollments = enrollments,
            Courses = courses
        };
    }
}
