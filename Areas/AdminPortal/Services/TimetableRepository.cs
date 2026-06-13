using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TimetableRepository : ITimetableRepository
{
    private const string SemesterConfigKey = "Semester";

    private static readonly string[] DefaultSemesters = ["Fall", "Spring", "Summer"];
    private static readonly string[] DefaultDaysOfWeek =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public TimetableRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<TimetableLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string classesSql = """
            SELECT ClassID, ClassName
            FROM dbo.Classes
            WHERE IsActive = 1
            ORDER BY SortOrder, ClassName;
            """;

        const string teachersSql = """
            SELECT TeacherID, ISNULL(EmployeeNo + N' · ', N'') + TeacherName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY EmployeeNo;
            """;

        const string yearsSql = """
            SELECT DISTINCT AcademicYear
            FROM dbo.TeacherCourseAssignments
            WHERE IsActive = 1
            ORDER BY AcademicYear DESC;
            """;

        var classes = new List<StudentLookupItem>();
        var teachers = new List<StudentLookupItem>();
        var years = new List<short>();

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

        await using (var command = new SqlCommand(teachersSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                teachers.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        await using (var command = new SqlCommand(yearsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                years.Add(Convert.ToInt16(reader[0]));
            }
        }

        if (years.Count == 0)
        {
            years.Add((short)DateTime.UtcNow.Year);
        }

        var semesters = await _configurations.GetValuesAsync(SemesterConfigKey, cancellationToken);

        return new TimetableLookups
        {
            Classes = classes,
            Teachers = teachers,
            Semesters = semesters.Count > 0 ? semesters : DefaultSemesters,
            AcademicYears = years,
            DaysOfWeek = DefaultDaysOfWeek
        };
    }

    public async Task<IReadOnlyList<TimetableSlotListItem>> ListAsync(
        int? classId,
        int? teacherId,
        string semester,
        short academicYear,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                tca.Uid,
                c.ClassID AS ClassId,
                c.ClassName,
                c.ClassCode,
                co.CourseID AS CourseId,
                co.CourseCode,
                co.CourseName,
                t.TeacherID AS TeacherId,
                t.TeacherName,
                t.EmployeeNo,
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
            INNER JOIN dbo.Teachers t ON tca.TeacherID = t.TeacherID
            WHERE tca.Semester = @Semester
              AND tca.AcademicYear = @AcademicYear
              AND (@ClassId IS NULL OR tca.ClassID = @ClassId)
              AND (@TeacherId IS NULL OR tca.TeacherID = @TeacherId)
            """ + (activeOnly ? " AND tca.IsActive = 1" : "") + """
             ORDER BY
                CASE tca.DayOfWeek
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'Sunday' THEN 7
                    ELSE 8
                END,
                tca.StartTime,
                c.ClassName,
                co.CourseCode;
            """;

        var list = new List<TimetableSlotListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassId", (object?)classId ?? DBNull.Value);
        command.Parameters.AddWithValue("@TeacherId", (object?)teacherId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Semester", semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", academicYear);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TimetableSlotListItem
            {
                AssignmentUid = Convert.ToInt32(reader["Uid"]),
                ClassId = Convert.ToInt32(reader["ClassId"]),
                ClassName = reader["ClassName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                CourseId = Convert.ToInt32(reader["CourseId"]),
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                TeacherId = Convert.ToInt32(reader["TeacherId"]),
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                EmployeeCode = reader["EmployeeNo"] as string ?? string.Empty,
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
}
