using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeacherAssignmentRepository : ITeacherAssignmentRepository
{
    private readonly string _connectionString;

    public TeacherAssignmentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<TeacherAssignmentListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ta.UID,
                ay.YearName,
                t.TeacherName,
                c.CourseCode,
                c.CourseName,
                CASE
                    WHEN cs.ClassSectionID IS NULL THEN NULL
                    ELSE ay2.YearName + N' · ' + cl.ClassName + N' · ' + s.SectionName
                END AS ClassSectionDisplay
            FROM dbo.TeacherAssignments ta
            INNER JOIN dbo.AcademicYears ay ON ta.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Teachers t ON ta.TeacherID = t.TeacherID
            INNER JOIN dbo.Courses c ON ta.CourseID = c.CourseID
            LEFT JOIN dbo.ClassSections cs ON ta.ClassSectionID = cs.ClassSectionID
            LEFT JOIN dbo.AcademicYears ay2 ON cs.AcademicYearID = ay2.AcademicYearID
            LEFT JOIN dbo.Classes cl ON cs.ClassID = cl.ClassID
            LEFT JOIN dbo.Sections s ON cs.SectionID = s.SectionID
            WHERE (@Search IS NULL
                   OR ay.YearName LIKE @Search
                   OR t.TeacherName LIKE @Search
                   OR t.EmployeeNo LIKE @Search
                   OR c.CourseCode LIKE @Search
                   OR c.CourseName LIKE @Search
                   OR cl.ClassName LIKE @Search
                   OR s.SectionName LIKE @Search)
            ORDER BY ay.YearName DESC, t.TeacherName, c.CourseCode;
            """;

        var list = new List<TeacherAssignmentListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TeacherAssignmentListItem
            {
                TeacherAssignmentId = Convert.ToInt32(reader["UID"]),
                YearName = reader["YearName"] as string ?? string.Empty,
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                ClassSectionDisplay = reader["ClassSectionDisplay"] as string
            });
        }

        return list;
    }

    public async Task<TeacherAssignmentFormModel?> GetAsync(int teacherAssignmentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, AcademicYearID, TeacherID, CourseID, ClassSectionID
            FROM dbo.TeacherAssignments
            WHERE UID = @TeacherAssignmentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherAssignmentId", teacherAssignmentId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TeacherAssignmentFormModel
        {
            TeacherAssignmentId = Convert.ToInt32(reader["UID"]),
            AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
            TeacherId = Convert.ToInt32(reader["TeacherID"]),
            CourseId = Convert.ToInt32(reader["CourseID"]),
            ClassSectionId = reader["ClassSectionID"] is DBNull ? null : Convert.ToInt32(reader["ClassSectionID"])
        };
    }

    public async Task<TeacherAssignmentLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string yearsSql = """
            SELECT AcademicYearID, YearName
            FROM dbo.AcademicYears
            ORDER BY YearName DESC;
            """;

        const string teachersSql = """
            SELECT TeacherID, ISNULL(EmployeeNo + N' · ', N'') + TeacherName
            FROM dbo.Teachers
            WHERE IsActive = 1
            ORDER BY TeacherName;
            """;

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + N' · ', N'') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseCode, CourseName;
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new TeacherAssignmentLookups
        {
            AcademicYears = await ReadLookupAsync(connection, yearsSql, cancellationToken),
            Teachers = await ReadLookupAsync(connection, teachersSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken),
            ClassSections = await ReadLookupAsync(connection, classSectionsSql, cancellationToken)
        };
    }

    public async Task<int> InsertAsync(TeacherAssignmentFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.TeacherAssignments (AcademicYearID, TeacherID, CourseID, ClassSectionID)
            VALUES (@AcademicYearId, @TeacherId, @CourseId, @ClassSectionId);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(TeacherAssignmentFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.TeacherAssignments SET
                AcademicYearID = @AcademicYearId,
                TeacherID = @TeacherId,
                CourseID = @CourseId,
                ClassSectionID = @ClassSectionId
            WHERE UID = @TeacherAssignmentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherAssignmentId", model.TeacherAssignmentId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int teacherAssignmentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.TeacherAssignments WHERE UID = @TeacherAssignmentId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherAssignmentId", teacherAssignmentId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, TeacherAssignmentFormModel model)
    {
        command.Parameters.AddWithValue("@AcademicYearId", model.AcademicYearId);
        command.Parameters.AddWithValue("@TeacherId", model.TeacherId);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
        command.Parameters.AddWithValue("@ClassSectionId", (object?)model.ClassSectionId ?? DBNull.Value);
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
