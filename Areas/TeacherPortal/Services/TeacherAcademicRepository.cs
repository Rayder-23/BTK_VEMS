using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.TeacherPortal.Models;

namespace VEMS.Areas.TeacherPortal.Services;

public sealed class TeacherAcademicRepository : ITeacherAcademicRepository
{
    private readonly string _connectionString;

    public TeacherAcademicRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<int?> ResolveTeacherIdAsync(
        string? employeeCode,
        int? employeeUid,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            await using var byCode = new SqlCommand(
                "SELECT TOP (1) TeacherID FROM dbo.Teachers WHERE EmployeeNo = @EmployeeNo;",
                connection);
            byCode.Parameters.AddWithValue("@EmployeeNo", employeeCode.Trim());
            var byCodeResult = await byCode.ExecuteScalarAsync(cancellationToken);
            if (byCodeResult is not null and not DBNull)
            {
                return Convert.ToInt32(byCodeResult);
            }
        }

        if (employeeUid is > 0)
        {
            await using var byEmployee = new SqlCommand(
                """
                SELECT TOP (1) t.TeacherID
                FROM dbo.Teachers t
                INNER JOIN dbo.Employee e ON e.EmployeeId = t.EmployeeNo
                WHERE e.Uid = @EmployeeUid;
                """,
                connection);
            byEmployee.Parameters.AddWithValue("@EmployeeUid", employeeUid.Value);
            var byEmployeeResult = await byEmployee.ExecuteScalarAsync(cancellationToken);
            if (byEmployeeResult is not null and not DBNull)
            {
                return Convert.ToInt32(byEmployeeResult);
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<ClassListItem>> ListAssignedClassesAsync(
        int teacherId,
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT DISTINCT
                c.ClassID,
                c.ClassName,
                c.ClassCode,
                c.SortOrder,
                c.IsActive
            FROM dbo.TeacherCourseAssignments tca
            INNER JOIN dbo.Classes c ON tca.ClassID = c.ClassID
            WHERE tca.TeacherID = @TeacherId
              AND tca.IsActive = 1
              AND (@Search IS NULL
                   OR c.ClassName LIKE @Search
                   OR c.ClassCode LIKE @Search)
            """ + (activeOnly ? " AND c.IsActive = 1" : "") + """
             ORDER BY c.SortOrder, c.ClassName;
            """;

        return await ReadClassesAsync(sql, teacherId, search, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseListItem>> ListAssignedCoursesAsync(
        int teacherId,
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT DISTINCT
                co.CourseID,
                co.CourseCode,
                co.CourseName,
                co.CreditHours,
                co.IsActive
            FROM dbo.TeacherCourseAssignments tca
            INNER JOIN dbo.Courses co ON tca.CourseID = co.CourseID
            WHERE tca.TeacherID = @TeacherId
              AND tca.IsActive = 1
              AND (@Search IS NULL
                   OR co.CourseCode LIKE @Search
                   OR co.CourseName LIKE @Search)
            """ + (activeOnly ? " AND co.IsActive = 1" : "") + """
             ORDER BY co.CourseCode, co.CourseName;
            """;

        var list = new List<CourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CourseListItem
            {
                CourseId = Convert.ToInt32(reader["CourseID"]),
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                CreditHours = reader["CreditHours"] is DBNull ? null : Convert.ToInt32(reader["CreditHours"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    private async Task<IReadOnlyList<ClassListItem>> ReadClassesAsync(
        string sql,
        int teacherId,
        string? search,
        CancellationToken cancellationToken)
    {
        var list = new List<ClassListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ClassListItem
            {
                ClassId = Convert.ToInt32(reader["ClassID"]),
                ClassName = reader["ClassName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                SortOrder = reader["SortOrder"] is DBNull ? null : Convert.ToInt32(reader["SortOrder"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }
}
