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
                "SELECT TOP (1) Uid FROM dbo.Teachers WHERE EmployeeCode = @EmployeeCode;",
                connection);
            byCode.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
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
                SELECT TOP (1) t.Uid
                FROM dbo.Teachers t
                INNER JOIN dbo.Employee e ON e.EmployeeId = t.EmployeeCode
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
                c.Uid,
                c.ClassName,
                c.ClassCode,
                p.ProgramName,
                c.SemesterNo,
                c.Semester,
                c.AcademicYear,
                c.Section,
                c.Shift,
                c.RoomNo,
                c.MaxStrength,
                c.IsActive
            FROM dbo.TeacherCourseAssignments tca
            INNER JOIN dbo.Classes c ON tca.ClassID = c.Uid
            INNER JOIN dbo.ref_Programs p ON c.ProgramID = p.Uid
            WHERE tca.TeacherID = @TeacherId
              AND tca.IsActive = 1
              AND (@Search IS NULL
                   OR c.ClassName LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR p.ProgramName LIKE @Search
                   OR c.Section LIKE @Search)
            """ + (activeOnly ? " AND c.IsActive = 1" : "") + """
             ORDER BY c.AcademicYear DESC, c.ClassCode;
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
                co.Uid,
                co.CourseCode,
                co.CourseTitle,
                p.ProgramName,
                co.CreditHours,
                co.CourseType,
                co.CourseLevel,
                co.IsActive
            FROM dbo.TeacherCourseAssignments tca
            INNER JOIN dbo.Courses co ON tca.CourseID = co.Uid
            INNER JOIN dbo.ref_Programs p ON co.ProgramID = p.Uid
            WHERE tca.TeacherID = @TeacherId
              AND tca.IsActive = 1
              AND (@Search IS NULL
                   OR co.CourseCode LIKE @Search
                   OR co.CourseTitle LIKE @Search
                   OR co.ShortName LIKE @Search
                   OR p.ProgramName LIKE @Search)
            """ + (activeOnly ? " AND co.IsActive = 1" : "") + """
             ORDER BY co.CourseCode;
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
                Uid = Convert.ToInt32(reader["Uid"]),
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                CreditHours = Convert.ToByte(reader["CreditHours"]),
                CourseType = reader["CourseType"] as string ?? string.Empty,
                CourseLevel = reader["CourseLevel"] as string ?? string.Empty,
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
                Uid = Convert.ToInt32(reader["Uid"]),
                ClassName = reader["ClassName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                SemesterNo = Convert.ToByte(reader["SemesterNo"]),
                Semester = reader["Semester"] as string ?? string.Empty,
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                Section = reader["Section"] as string,
                Shift = reader["Shift"] as string,
                RoomNo = reader["RoomNo"] as string,
                MaxStrength = Convert.ToInt16(reader["MaxStrength"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }
}
