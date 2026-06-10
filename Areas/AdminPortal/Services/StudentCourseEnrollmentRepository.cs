using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentCourseEnrollmentRepository : IStudentCourseEnrollmentRepository
{
    public static readonly IReadOnlyList<string> AllowedEnrollmentTypes = ["Manual", "Auto"];
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
                s.FirstName + ' ' + s.LastName AS StudentName,
                s.RegistrationNo,
                c.ClassCode,
                co.CourseCode,
                co.CourseTitle,
                se.AcademicYear,
                se.GradeOrSemester,
                sce.EnrollmentType,
                sce.Status,
                sce.IsActive
            FROM dbo.StudentCourseEnrollments sce
            INNER JOIN dbo.Students s ON sce.StudentID = s.Uid
            INNER JOIN dbo.StudentEnrollments se ON sce.EnrollmentID = se.Uid
            INNER JOIN dbo.ClassCourses cc ON sce.ClassCourseID = cc.Uid
            INNER JOIN dbo.Classes c ON cc.ClassID = c.Uid
            INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
            WHERE (@Search IS NULL
                   OR s.FirstName LIKE @Search
                   OR s.LastName LIKE @Search
                   OR s.RegistrationNo LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR co.CourseTitle LIKE @Search)
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
                CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                EnrollmentType = reader["EnrollmentType"] as string ?? string.Empty,
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
                sce.ClassCourseID,
                sce.EnrollmentType,
                sce.Status,
                sce.IsActive,
                sce.Remarks,
                s.RegistrationNo + ' - ' + s.FirstName + ' ' + s.LastName AS StudentDisplay,
                c.ClassCode + ' / ' + co.CourseCode + ' - ' + co.CourseTitle AS ClassCourseDisplay
            FROM dbo.StudentCourseEnrollments sce
            INNER JOIN dbo.Students s ON sce.StudentID = s.Uid
            INNER JOIN dbo.ClassCourses cc ON sce.ClassCourseID = cc.Uid
            INNER JOIN dbo.Classes c ON cc.ClassID = c.Uid
            INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
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
            ClassCourseId = Convert.ToInt32(reader["ClassCourseID"]),
            EnrollmentType = reader["EnrollmentType"] as string ?? string.Empty,
            Status = reader["Status"] as string ?? string.Empty,
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            Remarks = reader["Remarks"] as string,
            StudentDisplay = reader["StudentDisplay"] as string,
            ClassCourseDisplay = reader["ClassCourseDisplay"] as string
        };
    }

    public async Task<StudentCourseEnrollmentLookups> GetLookupsAsync(
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        const string studentsSql = """
            SELECT Uid, RegistrationNo + ' - ' + FirstName + ' ' + LastName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY RegistrationNo;
            """;

        const string classCoursesSql = """
            SELECT
                cc.Uid,
                c.ClassCode + ' / ' + co.CourseCode + ' - ' + co.CourseTitle
            FROM dbo.ClassCourses cc
            INNER JOIN dbo.Classes c ON cc.ClassID = c.Uid
            INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
            WHERE cc.IsActive = 1
            ORDER BY c.ClassCode, co.CourseCode;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var students = await ReadLookupAsync(connection, studentsSql, cancellationToken);
        var classCourses = await ReadLookupAsync(connection, classCoursesSql, cancellationToken);
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
            ClassCourses = classCourses,
            ProgramEnrollments = programEnrollments,
            EnrollmentTypes = AllowedEnrollmentTypes,
            Statuses = statuses
        };
    }

    public async Task<bool> ExistsAsync(int studentId, int classCourseId, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentCourseEnrollments
            WHERE StudentID = @StudentID
              AND ClassCourseID = @ClassCourseID
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentID", studentId);
        command.Parameters.AddWithValue("@ClassCourseID", classCourseId);
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

    public async Task<int> InsertAsync(StudentCourseEnrollmentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.StudentCourseEnrollments (
                EnrollmentID,
                StudentID,
                ClassCourseID,
                EnrollmentType,
                Status,
                IsActive,
                Remarks,
                CreatedBy,
                CreatedAt
            )
            VALUES (
                @EnrollmentID,
                @StudentID,
                @ClassCourseID,
                @EnrollmentType,
                @Status,
                @IsActive,
                @Remarks,
                @CreatedBy,
                SYSUTCDATETIME()
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(StudentCourseEnrollmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentCourseEnrollments SET
                EnrollmentID = @EnrollmentID,
                StudentID = @StudentID,
                ClassCourseID = @ClassCourseID,
                EnrollmentType = @EnrollmentType,
                Status = @Status,
                IsActive = @IsActive,
                Remarks = @Remarks,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentCourseEnrollments SET
                IsActive = 0,
                Status = 'Dropped',
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, StudentCourseEnrollmentFormModel model)
    {
        command.Parameters.AddWithValue("@EnrollmentID", model.EnrollmentId);
        command.Parameters.AddWithValue("@StudentID", model.StudentId);
        command.Parameters.AddWithValue("@ClassCourseID", model.ClassCourseId);
        command.Parameters.AddWithValue("@EnrollmentType", model.EnrollmentType.Trim());
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
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
            INNER JOIN dbo.ref_Programs p ON se.ProgramID = p.Uid
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
