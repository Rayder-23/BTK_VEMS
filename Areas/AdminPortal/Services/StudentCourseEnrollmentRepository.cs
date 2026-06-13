using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentCourseEnrollmentRepository : IStudentCourseEnrollmentRepository
{
    public static readonly IReadOnlyList<string> AllowedStatuses = ["Active", "Dropped", "Completed", "Suspended"];

    private const string EnrollmentStatusConfigKey = "EnrollmentStatus";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public StudentCourseEnrollmentRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<StudentCourseEnrollmentListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                sce.Uid,
                s.StudentName,
                s.RegistrationNo,
                c.ClassCode,
                co.CourseCode,
                co.CourseName,
                se.AcademicYear,
                se.GradeOrSemester,
                sce.Status,
                sce.IsActive
            FROM dbo.StudentCourseEnrollments sce
            INNER JOIN dbo.Students s ON sce.StudentID = s.StudentID
            INNER JOIN dbo.StudentEnrollments se ON sce.EnrollmentID = se.Uid
            INNER JOIN dbo.ClassSectionCourses csc ON sce.ClassSectionCourseID = csc.UID
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            WHERE (@Search IS NULL
                   OR s.StudentName LIKE @Search
                   OR s.RegistrationNo LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR co.CourseName LIKE @Search)
            """ + (activeOnly ? " AND sce.IsActive = 1" : "") + """
             ORDER BY s.RegistrationNo, c.ClassCode, co.CourseCode;
            """;

        var list = new List<StudentCourseEnrollmentListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentCourseEnrollmentListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                StudentName = reader["StudentName"] as string ?? string.Empty,
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                Status = reader["Status"] as string ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<StudentCourseEnrollmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                sce.Uid,
                sce.EnrollmentID,
                sce.StudentID,
                sce.ClassSectionCourseID,
                sce.Status,
                sce.IsActive,
                s.RegistrationNo + ' - ' + s.StudentName AS StudentDisplay,
                c.ClassName + ' / ' + co.CourseCode + ' - ' + co.CourseName AS ClassSectionCourseDisplay
            FROM dbo.StudentCourseEnrollments sce
            INNER JOIN dbo.Students s ON sce.StudentID = s.StudentID
            INNER JOIN dbo.ClassSectionCourses csc ON sce.ClassSectionCourseID = csc.UID
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            WHERE sce.Uid = @Uid;
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

        return new StudentCourseEnrollmentFormModel
        {
            Uid = Convert.ToInt32(reader["Uid"]),
            EnrollmentId = Convert.ToInt32(reader["EnrollmentID"]),
            StudentId = Convert.ToInt32(reader["StudentID"]),
            ClassSectionCourseId = Convert.ToInt32(reader["ClassSectionCourseID"]),
            Status = reader["Status"] as string ?? string.Empty,
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            StudentDisplay = reader["StudentDisplay"] as string,
            ClassSectionCourseDisplay = reader["ClassSectionCourseDisplay"] as string
        };
    }

    public async Task<StudentCourseEnrollmentLookups> GetLookupsAsync(
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        const string studentsSql = """
            SELECT StudentID, RegistrationNo + ' - ' + StudentName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY RegistrationNo;
            """;

        const string classSectionCoursesSql = """
            SELECT
                csc.UID,
                c.ClassName + N' / ' + s.SectionName + N' / ' + co.CourseCode + N' - ' + co.CourseName
            FROM dbo.ClassSectionCourses csc
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Sections s ON cs.SectionID = s.SectionID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            ORDER BY c.ClassName, s.SectionName, co.CourseCode;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var students = await ReadLookupAsync(connection, studentsSql, cancellationToken);
        var classSectionCourses = await ReadLookupAsync(connection, classSectionCoursesSql, cancellationToken);
        var programEnrollments = studentId is > 0
            ? await ReadProgramEnrollmentsAsync(connection, studentId.Value, cancellationToken)
            : [];

        var configStatuses = await _configurations.GetValuesAsync(EnrollmentStatusConfigKey, cancellationToken);
        var statuses = configStatuses
            .Where(s => AllowedStatuses.Any(a => string.Equals(a, s, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (statuses.Count == 0)
        {
            statuses = AllowedStatuses.ToList();
        }

        return new StudentCourseEnrollmentLookups
        {
            Students = students,
            ClassSectionCourses = classSectionCourses,
            ProgramEnrollments = programEnrollments,
            Statuses = statuses
        };
    }

    public async Task<bool> ExistsAsync(int studentId, int classSectionCourseId, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentCourseEnrollments
            WHERE StudentID = @StudentID
              AND ClassSectionCourseID = @ClassSectionCourseID
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentID", studentId);
        command.Parameters.AddWithValue("@ClassSectionCourseID", classSectionCourseId);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> EnrollmentBelongsToStudentAsync(
        int enrollmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentEnrollments
            WHERE Uid = @EnrollmentID
              AND StudentID = @StudentID;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EnrollmentID", enrollmentId);
        command.Parameters.AddWithValue("@StudentID", studentId);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(StudentCourseEnrollmentFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.StudentCourseEnrollments (
                EnrollmentID,
                StudentID,
                ClassSectionCourseID,
                Status,
                IsActive
            )
            VALUES (
                @EnrollmentID,
                @StudentID,
                @ClassSectionCourseID,
                @Status,
                @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(StudentCourseEnrollmentFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentCourseEnrollments SET
                EnrollmentID = @EnrollmentID,
                StudentID = @StudentID,
                ClassSectionCourseID = @ClassSectionCourseID,
                Status = @Status,
                IsActive = @IsActive
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentCourseEnrollments SET
                IsActive = 0,
                Status = 'Dropped'
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, StudentCourseEnrollmentFormModel model)
    {
        command.Parameters.AddWithValue("@EnrollmentID", model.EnrollmentId);
        command.Parameters.AddWithValue("@StudentID", model.StudentId);
        command.Parameters.AddWithValue("@ClassSectionCourseID", model.ClassSectionCourseId);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadProgramEnrollmentsAsync(
        SqlConnection connection,
        int studentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                se.Uid,
                p.ProgramCode + ' · ' + CAST(se.AcademicYear AS nvarchar(4))
                    + ' · Sem ' + CAST(se.GradeOrSemester AS nvarchar(3))
                    + ' (' + se.RollNo + ')'
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            WHERE se.StudentID = @StudentID
              AND se.EnrollmentStatus = 'Active'
            ORDER BY se.AcademicYear DESC, se.GradeOrSemester;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentID", studentId);
        return await ReadLookupFromCommandAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection);
        return await ReadLookupFromCommandAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupFromCommandAsync(
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentLookupItem
            {
                Id = Convert.ToInt32(reader[0]),
                Name = reader[1] as string ?? string.Empty
            });
        }

        return list;
    }
}
