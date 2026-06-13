using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TimetablesRepository : ITimetablesRepository
{
    private static readonly string[] DefaultDaysOfWeek =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    private readonly string _connectionString;

    public TimetablesRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<TimetableListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                t.TimetableID,
                t.DayName,
                ISNULL(p.PeriodName, N'Period ' + CAST(p.PeriodID AS nvarchar(10)))
                    + CASE
                        WHEN p.StartTime IS NULL AND p.EndTime IS NULL THEN N''
                        ELSE N' (' + CONVERT(nvarchar(5), p.StartTime, 108) + N'–' + CONVERT(nvarchar(5), p.EndTime, 108) + N')'
                      END AS PeriodDisplay,
                CASE
                    WHEN cs.ClassSectionID IS NULL THEN NULL
                    ELSE ay1.YearName + N' · ' + c.ClassName + N' · ' + sec.SectionName
                END AS ClassSectionDisplay,
                CASE
                    WHEN csec.CourseSectionID IS NULL THEN NULL
                    ELSE ay2.YearName + N' · ' + ISNULL(co2.CourseCode + N' · ', N'') + co2.CourseName
                        + CASE WHEN csec.SectionName IS NULL OR csec.SectionName = N'' THEN N'' ELSE N' · ' + csec.SectionName END
                END AS CourseSectionDisplay,
                ISNULL(co.CourseCode + N' · ', N'') + co.CourseName AS CourseName,
                ISNULL(te.EmployeeNo + N' · ', N'') + te.TeacherName AS TeacherName,
                t.RoomNo
            FROM dbo.Timetables t
            INNER JOIN dbo.Periods p ON t.PeriodID = p.PeriodID
            INNER JOIN dbo.Courses co ON t.CourseID = co.CourseID
            INNER JOIN dbo.Teachers te ON t.TeacherID = te.TeacherID
            LEFT JOIN dbo.ClassSections cs ON t.ClassSectionID = cs.ClassSectionID
            LEFT JOIN dbo.AcademicYears ay1 ON cs.AcademicYearID = ay1.AcademicYearID
            LEFT JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            LEFT JOIN dbo.Sections sec ON cs.SectionID = sec.SectionID
            LEFT JOIN dbo.CourseSections csec ON t.CourseSectionID = csec.CourseSectionID
            LEFT JOIN dbo.AcademicYears ay2 ON csec.AcademicYearID = ay2.AcademicYearID
            LEFT JOIN dbo.Courses co2 ON csec.CourseID = co2.CourseID
            WHERE (@Search IS NULL
                   OR t.DayName LIKE @Search
                   OR p.PeriodName LIKE @Search
                   OR co.CourseName LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR te.TeacherName LIKE @Search
                   OR te.EmployeeNo LIKE @Search
                   OR t.RoomNo LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR sec.SectionName LIKE @Search
                   OR csec.SectionName LIKE @Search)
            ORDER BY
                CASE t.DayName
                    WHEN 'Monday' THEN 1
                    WHEN 'Tuesday' THEN 2
                    WHEN 'Wednesday' THEN 3
                    WHEN 'Thursday' THEN 4
                    WHEN 'Friday' THEN 5
                    WHEN 'Saturday' THEN 6
                    WHEN 'Sunday' THEN 7
                    ELSE 8
                END,
                p.StartTime,
                co.CourseName;
            """;

        var list = new List<TimetableListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TimetableListItem
            {
                TimetableId = Convert.ToInt32(reader["TimetableID"]),
                DayName = reader["DayName"] as string ?? string.Empty,
                PeriodDisplay = reader["PeriodDisplay"] as string ?? string.Empty,
                ClassSectionDisplay = reader["ClassSectionDisplay"] as string,
                CourseSectionDisplay = reader["CourseSectionDisplay"] as string,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                RoomNo = reader["RoomNo"] as string
            });
        }

        return list;
    }

    public async Task<TimetableFormModel?> GetAsync(int timetableId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                TimetableID,
                DayName,
                PeriodID,
                ClassSectionID,
                CourseSectionID,
                CourseID,
                TeacherID,
                RoomNo
            FROM dbo.Timetables
            WHERE TimetableID = @TimetableId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TimetableId", timetableId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapForm(reader);
    }

    public async Task<TimetablesLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string periodsSql = """
            SELECT
                PeriodID,
                ISNULL(PeriodName, N'Period ' + CAST(PeriodID AS nvarchar(10)))
                    + CASE
                        WHEN StartTime IS NULL AND EndTime IS NULL THEN N''
                        ELSE N' (' + CONVERT(nvarchar(5), StartTime, 108) + N'–' + CONVERT(nvarchar(5), EndTime, 108) + N')'
                      END
            FROM dbo.Periods
            ORDER BY StartTime, PeriodName;
            """;

        const string classSectionsSql = """
            SELECT
                cs.ClassSectionID,
                ay.YearName + N' · ' + c.ClassName + N' · ' + s.SectionName
            FROM dbo.ClassSections cs
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Sections s ON cs.SectionID = s.SectionID
            ORDER BY ay.YearName DESC, c.ClassName, s.SectionName;
            """;

        const string courseSectionsSql = """
            SELECT
                cs.CourseSectionID,
                ay.YearName + N' · ' + ISNULL(c.CourseCode + N' · ', N'') + c.CourseName
                    + CASE WHEN cs.SectionName IS NULL OR cs.SectionName = N'' THEN N'' ELSE N' · ' + cs.SectionName END
            FROM dbo.CourseSections cs
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Courses c ON cs.CourseID = c.CourseID
            ORDER BY ay.YearName DESC, c.CourseName, cs.SectionName;
            """;

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + N' · ', N'') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseName;
            """;

        const string teachersSql = """
            SELECT TeacherID, ISNULL(EmployeeNo + N' · ', N'') + TeacherName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY EmployeeNo, TeacherName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new TimetablesLookups
        {
            DaysOfWeek = DefaultDaysOfWeek,
            Periods = await ReadLookupAsync(connection, periodsSql, cancellationToken),
            ClassSections = await ReadLookupAsync(connection, classSectionsSql, cancellationToken),
            CourseSections = await ReadLookupAsync(connection, courseSectionsSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken),
            Teachers = await ReadLookupAsync(connection, teachersSql, cancellationToken)
        };
    }

    public async Task<int> InsertAsync(TimetableFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Timetables (
                DayName, PeriodID, ClassSectionID, CourseSectionID, CourseID, TeacherID, RoomNo)
            VALUES (
                @DayName, @PeriodId, @ClassSectionId, @CourseSectionId, @CourseId, @TeacherId, @RoomNo);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(TimetableFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Timetables SET
                DayName = @DayName,
                PeriodID = @PeriodId,
                ClassSectionID = @ClassSectionId,
                CourseSectionID = @CourseSectionId,
                CourseID = @CourseId,
                TeacherID = @TeacherId,
                RoomNo = @RoomNo
            WHERE TimetableID = @TimetableId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TimetableId", model.TimetableId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int timetableId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Timetables WHERE TimetableID = @TimetableId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TimetableId", timetableId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, TimetableFormModel model)
    {
        command.Parameters.AddWithValue("@DayName", model.DayName.Trim());
        command.Parameters.AddWithValue("@PeriodId", model.PeriodId);
        command.Parameters.AddWithValue("@ClassSectionId", model.ClassSectionId.HasValue && model.ClassSectionId > 0 ? model.ClassSectionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@CourseSectionId", model.CourseSectionId.HasValue && model.CourseSectionId > 0 ? model.CourseSectionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
        command.Parameters.AddWithValue("@TeacherId", model.TeacherId);
        command.Parameters.AddWithValue("@RoomNo", string.IsNullOrWhiteSpace(model.RoomNo) ? DBNull.Value : model.RoomNo.Trim());
    }

    private static TimetableFormModel MapForm(SqlDataReader reader) => new()
    {
        TimetableId = Convert.ToInt32(reader["TimetableID"]),
        DayName = reader["DayName"] as string ?? string.Empty,
        PeriodId = Convert.ToInt32(reader["PeriodID"]),
        ClassSectionId = reader["ClassSectionID"] is DBNull ? null : Convert.ToInt32(reader["ClassSectionID"]),
        CourseSectionId = reader["CourseSectionID"] is DBNull ? null : Convert.ToInt32(reader["CourseSectionID"]),
        CourseId = Convert.ToInt32(reader["CourseID"]),
        TeacherId = Convert.ToInt32(reader["TeacherID"]),
        RoomNo = reader["RoomNo"] as string
    };

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var command = new SqlCommand(sql, connection);
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
