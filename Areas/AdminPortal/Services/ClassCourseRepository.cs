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
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                cc.Uid,
                c.ClassCode,
                c.ClassName,
                co.CourseCode,
                co.CourseTitle,
                NULLIF(LTRIM(RTRIM(t.FirstName + ' ' + t.LastName)), '') AS TeacherName,
                cc.IsActive,
                cc.CreatedAt
            FROM dbo.ClassCourses cc
            INNER JOIN dbo.Classes c ON cc.ClassID = c.Uid
            INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
            LEFT JOIN dbo.Teachers t ON cc.TeacherID = t.Uid
            WHERE (@Search IS NULL
                   OR c.ClassCode LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR co.CourseTitle LIKE @Search)
            """ + (activeOnly ? " AND cc.IsActive = 1" : "") + """
             ORDER BY c.ClassCode, co.CourseCode;
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
                Uid = Convert.ToInt32(reader["Uid"]),
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                ClassName = reader["ClassName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
                TeacherName = reader["TeacherName"] as string,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return list;
    }

    public async Task<ClassCourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                cc.Uid,
                cc.ClassID,
                cc.CourseID,
                cc.TeacherID,
                cc.IsActive,
                cc.CreatedAt,
                c.ClassCode + ' - ' + c.ClassName AS ClassDisplay,
                co.CourseCode + ' - ' + co.CourseTitle AS CourseDisplay
            FROM dbo.ClassCourses cc
            INNER JOIN dbo.Classes c ON cc.ClassID = c.Uid
            INNER JOIN dbo.Courses co ON cc.CourseID = co.Uid
            WHERE cc.Uid = @Uid;
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

        return new ClassCourseFormModel
        {
            Uid = Convert.ToInt32(reader["Uid"]),
            ClassId = Convert.ToInt32(reader["ClassID"]),
            CourseId = Convert.ToInt32(reader["CourseID"]),
            TeacherId = reader["TeacherID"] is DBNull ? null : Convert.ToInt32(reader["TeacherID"]),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            ClassDisplay = reader["ClassDisplay"] as string,
            CourseDisplay = reader["CourseDisplay"] as string
        };
    }

    public async Task<ClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string classesSql = """
            SELECT Uid, ClassCode + ' - ' + ClassName
            FROM dbo.Classes
            WHERE IsActive = 1
            ORDER BY ClassCode;
            """;

        const string coursesSql = """
            SELECT Uid, CourseCode + ' - ' + CourseTitle
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode;
            """;

        const string teachersSql = """
            SELECT Uid, EmployeeCode + ' - ' + FirstName + ' ' + LastName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY EmployeeCode;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new ClassCourseLookups
        {
            Classes = await ReadLookupAsync(connection, classesSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken),
            Teachers = await ReadLookupAsync(connection, teachersSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(int classId, int courseId, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ClassCourses
            WHERE ClassID = @ClassID
              AND CourseID = @CourseID
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassID", classId);
        command.Parameters.AddWithValue("@CourseID", courseId);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ClassCourses (
                ClassID,
                CourseID,
                TeacherID,
                IsActive
            )
            VALUES (
                @ClassID,
                @CourseID,
                @TeacherID,
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

    public async Task<bool> UpdateAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ClassCourses SET
                ClassID = @ClassID,
                CourseID = @CourseID,
                TeacherID = @TeacherID,
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
            UPDATE dbo.ClassCourses SET IsActive = 0
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, ClassCourseFormModel model)
    {
        command.Parameters.AddWithValue("@ClassID", model.ClassId);
        command.Parameters.AddWithValue("@CourseID", model.CourseId);
        command.Parameters.AddWithValue("@TeacherID", (object?)model.TeacherId ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
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
