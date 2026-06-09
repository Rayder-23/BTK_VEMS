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

        const string studentProgramSql = """
            SELECT
                p.ProgramName,
                p.ProgramCode,
                s.RollNo,
                s.AdmissionYear
            FROM dbo.Students s
            INNER JOIN dbo.ref_Programs p ON s.ProgramID = p.Uid
            WHERE s.Uid = @StudentUid;
            """;

        const string enrollmentSql = """
            SELECT
                p.ProgramName,
                e.AcademicYear,
                e.GradeOrSemester,
                e.RollNo
            FROM dbo.StudentEnrollments e
            INNER JOIN dbo.ref_Programs p ON p.Uid = e.ProgramID
            WHERE e.StudentID = @StudentUid
              AND e.EnrollmentStatus = @EnrollmentStatus
            ORDER BY e.AcademicYear DESC, e.GradeOrSemester;
            """;

        const string coursesSql = """
            SELECT DISTINCT
                c.Uid,
                c.CourseCode,
                c.CourseTitle,
                c.ShortName,
                p.ProgramName,
                c.CreditHours,
                c.TheoryHours,
                c.LabHours,
                c.CourseType,
                c.CourseLevel,
                c.SemesterNo,
                c.IsMandatory,
                c.Description,
                c.Objectives,
                e.AcademicYear,
                e.GradeOrSemester
            FROM dbo.StudentEnrollments e
            INNER JOIN dbo.Courses c ON c.ProgramID = e.ProgramID
            INNER JOIN dbo.ref_Programs p ON p.Uid = e.ProgramID
            WHERE e.StudentID = @StudentUid
              AND e.EnrollmentStatus = @EnrollmentStatus
              AND c.IsActive = 1
              AND (c.SemesterNo IS NULL OR c.SemesterNo = e.GradeOrSemester)
            ORDER BY e.AcademicYear DESC, c.SemesterNo, c.CourseCode;
            """;

        var enrollments = new List<StudentEnrollmentContext>();
        var courses = new List<StudentAssignedCourseItem>();
        string? programName = null;
        string? programCode = null;
        string? rollNo = null;
        short? admissionYear = null;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(studentProgramSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                programName = reader["ProgramName"] as string;
                programCode = reader["ProgramCode"] as string;
                rollNo = reader["RollNo"] as string;
                admissionYear = reader["AdmissionYear"] is DBNull ? null : Convert.ToInt16(reader["AdmissionYear"]);
            }
        }

        await using (var command = new SqlCommand(enrollmentSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            command.Parameters.AddWithValue("@EnrollmentStatus", activeEnrollmentStatus);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                enrollments.Add(new StudentEnrollmentContext
                {
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                    GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                    RollNo = reader["RollNo"] as string ?? string.Empty
                });
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
                    Uid = Convert.ToInt32(reader["Uid"]),
                    CourseCode = reader["CourseCode"] as string ?? string.Empty,
                    CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
                    ShortName = reader["ShortName"] as string,
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    CreditHours = Convert.ToByte(reader["CreditHours"]),
                    TheoryHours = Convert.ToByte(reader["TheoryHours"]),
                    LabHours = Convert.ToByte(reader["LabHours"]),
                    CourseType = reader["CourseType"] as string ?? string.Empty,
                    CourseLevel = reader["CourseLevel"] as string ?? string.Empty,
                    SemesterNo = reader["SemesterNo"] is DBNull ? null : Convert.ToByte(reader["SemesterNo"]),
                    IsMandatory = Convert.ToBoolean(reader["IsMandatory"]),
                    Description = reader["Description"] as string,
                    Objectives = reader["Objectives"] as string,
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
