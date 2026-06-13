using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeacherCourseLinkRepository : ITeacherCourseLinkRepository
{
    private readonly string _connectionString;

    public TeacherCourseLinkRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<TeacherCourseListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                tc.UID,
                t.TeacherName,
                c.CourseCode,
                c.CourseName
            FROM dbo.TeacherCourses tc
            INNER JOIN dbo.Teachers t ON tc.TeacherID = t.TeacherID
            INNER JOIN dbo.Courses c ON tc.CourseID = c.CourseID
            WHERE (@Search IS NULL
                   OR t.TeacherName LIKE @Search
                   OR t.EmployeeNo LIKE @Search
                   OR c.CourseName LIKE @Search
                   OR c.CourseCode LIKE @Search)
            ORDER BY t.TeacherName, c.CourseCode, c.CourseName;
            """;

        var list = new List<TeacherCourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TeacherCourseListItem
            {
                TeacherCourseId = Convert.ToInt32(reader["UID"]),
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<TeacherCourseFormModel?> GetAsync(int teacherCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, TeacherID, CourseID
            FROM dbo.TeacherCourses
            WHERE UID = @TeacherCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherCourseId", teacherCourseId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<TeacherCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string teachersSql = """
            SELECT TeacherID, ISNULL(EmployeeNo + ' · ', '') + TeacherName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY TeacherName;
            """;

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + ' · ', '') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode, CourseName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new TeacherCourseLookups
        {
            Teachers = await ReadLookupAsync(connection, teachersSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int teacherId,
        int courseId,
        int? excludeTeacherCourseId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TeacherCourses
            WHERE TeacherID = @TeacherId
              AND CourseID = @CourseId
              AND (@ExcludeTeacherCourseId IS NULL OR UID <> @ExcludeTeacherCourseId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@ExcludeTeacherCourseId", (object?)excludeTeacherCourseId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(TeacherCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.TeacherCourses (TeacherID, CourseID)
            VALUES (@TeacherId, @CourseId);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(TeacherCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TeacherCourses SET
                TeacherID = @TeacherId,
                CourseID = @CourseId
            WHERE UID = @TeacherCourseId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherCourseId", model.TeacherCourseId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int teacherCourseId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.TeacherCourses WHERE UID = @TeacherCourseId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherCourseId", teacherCourseId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, TeacherCourseFormModel model)
    {
        command.Parameters.AddWithValue("@TeacherId", model.TeacherId);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
    }

    private static TeacherCourseFormModel MapForm(SqlDataReader reader) => new()
    {
        TeacherCourseId = Convert.ToInt32(reader["UID"]),
        TeacherId = Convert.ToInt32(reader["TeacherID"]),
        CourseId = Convert.ToInt32(reader["CourseID"])
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
