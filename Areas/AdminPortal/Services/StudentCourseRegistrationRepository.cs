using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentCourseRegistrationRepository : IStudentCourseRegistrationRepository
{
    private readonly string _connectionString;

    public StudentCourseRegistrationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<StudentCourseRegistrationListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                scr.UID,
                st.RegistrationNo,
                st.StudentName,
                ay.YearName + N' · ' + ISNULL(c.CourseCode + N' · ', N'') + c.CourseName
                    + CASE WHEN cs.SectionName IS NULL OR cs.SectionName = N'' THEN N'' ELSE N' · ' + cs.SectionName END
                    AS CourseSectionDisplay,
                scr.RegistrationDate
            FROM dbo.StudentCourseRegistrations scr
            INNER JOIN dbo.Students st ON scr.StudentID = st.StudentID
            INNER JOIN dbo.CourseSections cs ON scr.CourseSectionID = cs.CourseSectionID
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Courses c ON cs.CourseID = c.CourseID
            WHERE (@Search IS NULL
                   OR st.RegistrationNo LIKE @Search
                   OR st.StudentName LIKE @Search
                   OR ay.YearName LIKE @Search
                   OR c.CourseName LIKE @Search
                   OR c.CourseCode LIKE @Search
                   OR cs.SectionName LIKE @Search)
            ORDER BY scr.RegistrationDate DESC, st.StudentName;
            """;

        var list = new List<StudentCourseRegistrationListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentCourseRegistrationListItem
            {
                Uid = Convert.ToInt32(reader["UID"]),
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                StudentName = reader["StudentName"] as string ?? string.Empty,
                CourseSectionDisplay = reader["CourseSectionDisplay"] as string ?? string.Empty,
                RegistrationDate = Convert.ToDateTime(reader["RegistrationDate"])
            });
        }

        return list;
    }

    public async Task<StudentCourseRegistrationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, StudentID, CourseSectionID, RegistrationDate
            FROM dbo.StudentCourseRegistrations
            WHERE UID = @Uid;
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

        return new StudentCourseRegistrationFormModel
        {
            Uid = Convert.ToInt32(reader["UID"]),
            StudentId = Convert.ToInt32(reader["StudentID"]),
            CourseSectionId = Convert.ToInt32(reader["CourseSectionID"]),
            RegistrationDate = Convert.ToDateTime(reader["RegistrationDate"])
        };
    }

    public async Task<StudentCourseRegistrationLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string studentsSql = """
            SELECT StudentID, ISNULL(RegistrationNo + N' · ', N'') + StudentName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY StudentName;
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new StudentCourseRegistrationLookups
        {
            Students = await ReadLookupAsync(connection, studentsSql, cancellationToken),
            CourseSections = await ReadLookupAsync(connection, courseSectionsSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int studentId,
        int courseSectionId,
        int? excludeUid,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentCourseRegistrations
            WHERE StudentID = @StudentId
              AND CourseSectionID = @CourseSectionId
              AND (@ExcludeUid IS NULL OR UID <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        command.Parameters.AddWithValue("@CourseSectionId", courseSectionId);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(StudentCourseRegistrationFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.StudentCourseRegistrations (StudentID, CourseSectionID, RegistrationDate)
            VALUES (@StudentId, @CourseSectionId, @RegistrationDate);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(StudentCourseRegistrationFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentCourseRegistrations SET
                StudentID = @StudentId,
                CourseSectionID = @CourseSectionId,
                RegistrationDate = @RegistrationDate
            WHERE UID = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.StudentCourseRegistrations WHERE UID = @Uid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, StudentCourseRegistrationFormModel model)
    {
        command.Parameters.AddWithValue("@StudentId", model.StudentId);
        command.Parameters.AddWithValue("@CourseSectionId", model.CourseSectionId);
        command.Parameters.AddWithValue("@RegistrationDate", model.RegistrationDate.Date);
    }

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
