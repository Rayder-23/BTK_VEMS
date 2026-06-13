using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentCourseRepository : IStudentCourseRepository
{
    private const string EnrollmentStatusConfigKey = "EnrollmentStatus";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public StudentCourseRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<StudentAllCoursesPageModel> GetAssignedCoursesAsync(
        int studentUid,
        CancellationToken cancellationToken = default)
    {
        var activeEnrollmentStatus = await ResolveActiveEnrollmentStatusAsync(cancellationToken);

        const string enrollmentSql = """
            SELECT
                p.ProgramName,
                p.ProgramCode,
                se.AcademicYear,
                se.GradeOrSemester,
                se.RollNo
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Programs p ON p.ProgramID = se.ProgramID
            WHERE se.StudentID = @StudentUid
              AND se.EnrollmentStatus = @EnrollmentStatus
            ORDER BY se.AcademicYear DESC, se.GradeOrSemester;
            """;

        const string coursesSql = """
            SELECT DISTINCT
                co.CourseID,
                co.CourseCode,
                co.CourseName,
                p.ProgramName,
                co.CreditHours,
                e.AcademicYear,
                e.GradeOrSemester
            FROM dbo.StudentCourseEnrollments sce
            INNER JOIN dbo.ClassSectionCourses csc ON sce.ClassSectionCourseID = csc.UID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            INNER JOIN dbo.StudentEnrollments e ON sce.EnrollmentID = e.Uid
            INNER JOIN dbo.Programs p ON e.ProgramID = p.ProgramID
            WHERE sce.StudentID = @StudentUid
              AND sce.IsActive = 1
              AND sce.Status = @EnrollmentStatus
              AND co.IsActive = 1
            ORDER BY e.AcademicYear DESC, co.CourseCode;
            """;

        var enrollments = new List<StudentEnrollmentContext>();
        var courses = new List<StudentAssignedCourseItem>();
        string? programName = null;
        string? programCode = null;
        string? rollNo = null;
        short? admissionYear = null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(enrollmentSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            command.Parameters.AddWithValue("@EnrollmentStatus", activeEnrollmentStatus);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var enrollment = new StudentEnrollmentContext
                {
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                    GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                    RollNo = reader["RollNo"] as string ?? string.Empty
                };
                enrollments.Add(enrollment);

                programName ??= reader["ProgramName"] as string;
                programCode ??= reader["ProgramCode"] as string;
                rollNo ??= reader["RollNo"] as string;
                admissionYear ??= Convert.ToInt16(reader["AcademicYear"]);
            }
        }

        await using (var command = new SqlCommand(coursesSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            command.Parameters.AddWithValue("@EnrollmentStatus", activeEnrollmentStatus);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                courses.Add(new StudentAssignedCourseItem
                {
                    CourseId = Convert.ToInt32(reader["CourseID"]),
                    CourseCode = reader["CourseCode"] as string ?? string.Empty,
                    CourseName = reader["CourseName"] as string ?? string.Empty,
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    CreditHours = reader["CreditHours"] is DBNull ? null : Convert.ToInt32(reader["CreditHours"]),
                    AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                    GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"])
                });
            }
        }

        var primaryEnrollment = enrollments.FirstOrDefault();

        return new StudentAllCoursesPageModel
        {
            ProgramName = programName ?? primaryEnrollment?.ProgramName,
            ProgramCode = programCode,
            RollNo = rollNo ?? primaryEnrollment?.RollNo,
            AdmissionYear = admissionYear,
            Enrollments = enrollments,
            Courses = courses
        };
    }

    private async Task<string> ResolveActiveEnrollmentStatusAsync(CancellationToken cancellationToken)
    {
        var statuses = await _configurations.GetValuesAsync(EnrollmentStatusConfigKey, cancellationToken);
        return statuses.FirstOrDefault(s => string.Equals(s, "Active", StringComparison.OrdinalIgnoreCase))
            ?? statuses.FirstOrDefault()
            ?? "Active";
    }
}
