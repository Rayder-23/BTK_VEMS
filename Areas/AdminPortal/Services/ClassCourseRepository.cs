using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ClassCourseRepository : IClassCourseRepository
{
    private readonly string _connectionString;

    public ClassCourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ClassCourseListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                csc.UID,
                ay.YearName,
                c.ClassName,
                s.SectionName,
                co.CourseCode,
                co.CourseName
            FROM dbo.ClassSectionCourses csc
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Sections s ON cs.SectionID = s.SectionID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            WHERE (@Search IS NULL
                   OR ay.YearName LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR s.SectionName LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR co.CourseName LIKE @Search)
            ORDER BY ay.YearName DESC, c.ClassName, s.SectionName, co.CourseCode;
            """;

        var list = new List<ClassCourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ClassCourseListItem
            {
                ClassSectionCourseId = Convert.ToInt32(reader["UID"]),
                YearName = reader["YearName"] as string ?? string.Empty,
                ClassName = reader["ClassName"] as string ?? string.Empty,
                SectionName = reader["SectionName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<ClassCourseFormModel?> GetAsync(int classSectionCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, ClassSectionID, CourseID
            FROM dbo.ClassSectionCourses
            WHERE UID = @ClassSectionCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionCourseId", classSectionCourseId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ClassCourseFormModel
        {
            ClassSectionCourseId = Convert.ToInt32(reader["UID"]),
            ClassSectionId = Convert.ToInt32(reader["ClassSectionID"]),
            CourseId = Convert.ToInt32(reader["CourseID"])
        };
    }

    public async Task<ClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
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

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + N' · ', N'') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode, CourseName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new ClassCourseLookups
        {
            ClassSections = await ReadLookupAsync(connection, classSectionsSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int classSectionId,
        int courseId,
        int? excludeClassSectionCourseId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ClassSectionCourses
            WHERE ClassSectionID = @ClassSectionID
              AND CourseID = @CourseID
              AND (@ExcludeClassSectionCourseId IS NULL OR UID <> @ExcludeClassSectionCourseId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionID", classSectionId);
        command.Parameters.AddWithValue("@CourseID", courseId);
        command.Parameters.AddWithValue("@ExcludeClassSectionCourseId", (object?)excludeClassSectionCourseId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ClassSectionCourses (ClassSectionID, CourseID)
            VALUES (@ClassSectionID, @CourseID);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ClassSectionCourses SET
                ClassSectionID = @ClassSectionID,
                CourseID = @CourseID
            WHERE UID = @ClassSectionCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionCourseId", model.ClassSectionCourseId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int classSectionCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.ClassSectionCourses WHERE UID = @ClassSectionCourseId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionCourseId", classSectionCourseId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, ClassCourseFormModel model)
    {
        command.Parameters.AddWithValue("@ClassSectionID", model.ClassSectionId);
        command.Parameters.AddWithValue("@CourseID", model.CourseId);
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
