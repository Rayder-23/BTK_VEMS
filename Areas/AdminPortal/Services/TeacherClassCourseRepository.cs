using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeacherClassCourseRepository : ITeacherClassCourseRepository
{
    public static readonly IReadOnlyList<string> AllowedRoles = ["Lead", "Assistant", "Co-Teacher", "Substitute"];

    private readonly string _connectionString;

    public TeacherClassCourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<TeacherClassCourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                tcc.Uid,
                t.EmployeeNo,
                t.TeacherName,
                c.ClassCode,
                c.ClassName,
                co.CourseCode,
                co.CourseName,
                tcc.Role,
                tcc.IsActive,
                tcc.CreatedAt
            FROM dbo.TeacherClassCourses tcc
            INNER JOIN dbo.Teachers t ON tcc.TeacherID = t.TeacherID
            INNER JOIN dbo.ClassSectionCourses csc ON tcc.ClassSectionCourseID = csc.UID
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            WHERE (@Search IS NULL
                   OR t.EmployeeNo LIKE @Search
                   OR t.TeacherName LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR co.CourseCode LIKE @Search
                   OR co.CourseName LIKE @Search
                   OR tcc.Role LIKE @Search)
            """ + (activeOnly ? " AND tcc.IsActive = 1" : "") + """
             ORDER BY t.EmployeeNo, c.ClassCode, co.CourseCode;
            """;

        var list = new List<TeacherClassCourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TeacherClassCourseListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                EmployeeCode = reader["EmployeeNo"] as string ?? string.Empty,
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                ClassName = reader["ClassName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                Role = reader["Role"] as string ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return list;
    }

    public async Task<TeacherClassCourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                tcc.Uid,
                tcc.TeacherID,
                tcc.ClassSectionCourseID,
                tcc.Role,
                tcc.IsActive,
                tcc.CreatedAt,
                ISNULL(t.EmployeeNo + N' - ', N'') + t.TeacherName AS TeacherDisplay,
                c.ClassName + N' / ' + co.CourseCode + N' - ' + co.CourseName AS ClassSectionCourseDisplay
            FROM dbo.TeacherClassCourses tcc
            INNER JOIN dbo.Teachers t ON tcc.TeacherID = t.TeacherID
            INNER JOIN dbo.ClassSectionCourses csc ON tcc.ClassSectionCourseID = csc.UID
            INNER JOIN dbo.ClassSections cs ON csc.ClassSectionID = cs.ClassSectionID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Courses co ON csc.CourseID = co.CourseID
            WHERE tcc.Uid = @Uid;
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

        return new TeacherClassCourseFormModel
        {
            Uid = Convert.ToInt32(reader["Uid"]),
            TeacherId = Convert.ToInt32(reader["TeacherID"]),
            ClassSectionCourseId = Convert.ToInt32(reader["ClassSectionCourseID"]),
            Role = reader["Role"] as string ?? "Lead",
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            TeacherDisplay = reader["TeacherDisplay"] as string,
            ClassSectionCourseDisplay = reader["ClassSectionCourseDisplay"] as string
        };
    }

    public async Task<TeacherClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string teachersSql = """
            SELECT TeacherID, ISNULL(EmployeeNo + N' · ', N'') + TeacherName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY EmployeeNo;
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

        return new TeacherClassCourseLookups
        {
            Teachers = await ReadLookupAsync(connection, teachersSql, cancellationToken),
            ClassSectionCourses = await ReadLookupAsync(connection, classSectionCoursesSql, cancellationToken),
            Roles = AllowedRoles.ToList()
        };
    }

    public async Task<bool> ExistsAsync(int teacherId, int classSectionCourseId, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.TeacherClassCourses
            WHERE TeacherID = @TeacherID
              AND ClassSectionCourseID = @ClassSectionCourseID
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherID", teacherId);
        command.Parameters.AddWithValue("@ClassSectionCourseID", classSectionCourseId);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(TeacherClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.TeacherClassCourses (
                TeacherID,
                ClassSectionCourseID,
                Role,
                IsActive
            )
            VALUES (
                @TeacherID,
                @ClassSectionCourseID,
                @Role,
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

    public async Task<bool> UpdateAsync(TeacherClassCourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TeacherClassCourses SET
                TeacherID = @TeacherID,
                ClassSectionCourseID = @ClassSectionCourseID,
                Role = @Role,
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
            UPDATE dbo.TeacherClassCourses SET IsActive = 0
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, TeacherClassCourseFormModel model)
    {
        command.Parameters.AddWithValue("@TeacherID", model.TeacherId);
        command.Parameters.AddWithValue("@ClassSectionCourseID", model.ClassSectionCourseId);
        command.Parameters.AddWithValue("@Role", model.Role.Trim());
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
