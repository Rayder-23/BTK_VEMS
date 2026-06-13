using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeacherCourseAssignmentRepository : ITeacherCourseAssignmentRepository
{
    private const string SemesterConfigKey = "Semester";

    private static readonly string[] DefaultSemesters = ["Fall", "Spring", "Summer"];
    private static readonly string[] DefaultDaysOfWeek =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public TeacherCourseAssignmentRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<TeacherAssignmentSummaryViewModel?> GetTeacherSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TeacherID, EmployeeNo, TeacherName
            FROM dbo.Teachers
            WHERE TeacherID = @TeacherId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TeacherAssignmentSummaryViewModel
        {
            TeacherId = Convert.ToInt32(reader["TeacherID"]),
            EmployeeCode = reader["EmployeeNo"] as string ?? string.Empty,
            FullName = reader["TeacherName"] as string ?? string.Empty
        };
    }

    public async Task<IReadOnlyList<TeacherCourseAssignmentListItem>> ListByTeacherAsync(
        int teacherId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                tca.Uid,
                c.ClassName,
                c.ClassCode,
                co.CourseName,
                co.CourseCode,
                tca.Semester,
                tca.AcademicYear,
                tca.DayOfWeek,
                tca.StartTime,
                tca.EndTime,
                tca.RoomNo,
                tca.IsActive
            FROM dbo.TeacherCourseAssignments tca
            INNER JOIN dbo.Classes c ON tca.ClassID = c.ClassID
            INNER JOIN dbo.Courses co ON tca.CourseID = co.CourseID
            WHERE tca.TeacherID = @TeacherId
            """ + (activeOnly ? " AND tca.IsActive = 1" : "") + """
             ORDER BY tca.AcademicYear DESC, tca.Semester, c.ClassCode, co.CourseCode, tca.DayOfWeek;
            """;

        var list = new List<TeacherCourseAssignmentListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TeacherCourseAssignmentListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                ClassName = reader["ClassName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                Semester = reader["Semester"] as string ?? string.Empty,
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                DayOfWeek = reader["DayOfWeek"] as string,
                StartTime = reader["StartTime"] is DBNull ? null : (TimeSpan)reader["StartTime"],
                EndTime = reader["EndTime"] is DBNull ? null : (TimeSpan)reader["EndTime"],
                RoomNo = reader["RoomNo"] as string,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<TeacherCourseAssignmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                TeacherID,
                ClassID,
                CourseID,
                Semester,
                AcademicYear,
                DayOfWeek,
                StartTime,
                EndTime,
                RoomNo,
                IsActive,
                Remarks
            FROM dbo.TeacherCourseAssignments
            WHERE Uid = @Uid;
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

        return Map(reader);
    }

    public async Task<TeacherCourseAssignmentLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string classesSql = """
            SELECT ClassID, ClassCode + ' · ' + ClassName
            FROM dbo.Classes
            WHERE IsActive = 1
            ORDER BY SortOrder, ClassCode;
            """;

        const string coursesSql = """
            SELECT CourseID, CourseCode + ' - ' + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode;
            """;

        var classes = new List<StudentLookupItem>();
        var courses = new List<StudentLookupItem>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(classesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                classes.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        await using (var command = new SqlCommand(coursesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                courses.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        var semesters = await _configurations.GetValuesAsync(SemesterConfigKey, cancellationToken);

        return new TeacherCourseAssignmentLookups
        {
            Classes = classes,
            Courses = courses,
            Semesters = semesters.Count > 0 ? semesters : DefaultSemesters,
            DaysOfWeek = DefaultDaysOfWeek
        };
    }

    public async Task<bool> AssignmentExistsAsync(
        TeacherCourseAssignmentFormModel model,
        int? excludeUid,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TeacherCourseAssignments
            WHERE TeacherID = @TeacherID
              AND ClassID = @ClassID
              AND CourseID = @CourseID
              AND Semester = @Semester
              AND AcademicYear = @AcademicYear
              AND (
                    (DayOfWeek IS NULL AND @DayOfWeek IS NULL)
                    OR DayOfWeek = @DayOfWeek
                  )
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherID", model.TeacherId);
        command.Parameters.AddWithValue("@ClassID", model.ClassId);
        command.Parameters.AddWithValue("@CourseID", model.CourseId);
        command.Parameters.AddWithValue("@Semester", model.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@DayOfWeek", (object?)model.DayOfWeek?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(TeacherCourseAssignmentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.TeacherCourseAssignments (
                TeacherID,
                ClassID,
                CourseID,
                Semester,
                AcademicYear,
                DayOfWeek,
                StartTime,
                EndTime,
                RoomNo,
                IsActive,
                Remarks,
                CreatedBy,
                CreatedAt
            )
            VALUES (
                @TeacherID,
                @ClassID,
                @CourseID,
                @Semester,
                @AcademicYear,
                @DayOfWeek,
                @StartTime,
                @EndTime,
                @RoomNo,
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

    public async Task<bool> UpdateAsync(TeacherCourseAssignmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TeacherCourseAssignments SET
                ClassID = @ClassID,
                CourseID = @CourseID,
                Semester = @Semester,
                AcademicYear = @AcademicYear,
                DayOfWeek = @DayOfWeek,
                StartTime = @StartTime,
                EndTime = @EndTime,
                RoomNo = @RoomNo,
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
            UPDATE dbo.TeacherCourseAssignments SET
                IsActive = 0,
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

    private static TeacherCourseAssignmentFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        TeacherId = Convert.ToInt32(reader["TeacherID"]),
        ClassId = Convert.ToInt32(reader["ClassID"]),
        CourseId = Convert.ToInt32(reader["CourseID"]),
        Semester = reader["Semester"] as string ?? string.Empty,
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        DayOfWeek = reader["DayOfWeek"] as string,
        StartTime = reader["StartTime"] is DBNull ? null : (TimeSpan)reader["StartTime"],
        EndTime = reader["EndTime"] is DBNull ? null : (TimeSpan)reader["EndTime"],
        RoomNo = reader["RoomNo"] as string,
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        Remarks = reader["Remarks"] as string
    };

    private static void Bind(SqlCommand command, TeacherCourseAssignmentFormModel model)
    {
        command.Parameters.AddWithValue("@TeacherID", model.TeacherId);
        command.Parameters.AddWithValue("@ClassID", model.ClassId);
        command.Parameters.AddWithValue("@CourseID", model.CourseId);
        command.Parameters.AddWithValue("@Semester", model.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@DayOfWeek", (object?)model.DayOfWeek?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@StartTime", model.StartTime.HasValue ? model.StartTime.Value : DBNull.Value);
        command.Parameters.AddWithValue("@EndTime", model.EndTime.HasValue ? model.EndTime.Value : DBNull.Value);
        command.Parameters.AddWithValue("@RoomNo", (object?)model.RoomNo?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
    }
}
