using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentEnrollmentLinkRepository : IStudentEnrollmentLinkRepository
{
    private readonly string _connectionString;

    public StudentEnrollmentLinkRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<StudentEnrollmentLinkListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                se.UID,
                ay.YearName,
                st.RegistrationNo,
                st.StudentName,
                p.ProgramName,
                CASE
                    WHEN cs.ClassSectionID IS NULL THEN NULL
                    ELSE ay2.YearName + N' · ' + c.ClassName + N' · ' + sec.SectionName
                END AS ClassSectionDisplay,
                se.RollNo,
                se.EnrollmentDate
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.AcademicYears ay ON se.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Students st ON se.StudentID = st.StudentID
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            LEFT JOIN dbo.ClassSections cs ON se.ClassSectionID = cs.ClassSectionID
            LEFT JOIN dbo.AcademicYears ay2 ON cs.AcademicYearID = ay2.AcademicYearID
            LEFT JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            LEFT JOIN dbo.Sections sec ON cs.SectionID = sec.SectionID
            WHERE (@Search IS NULL
                   OR ay.YearName LIKE @Search
                   OR st.RegistrationNo LIKE @Search
                   OR st.StudentName LIKE @Search
                   OR p.ProgramName LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR sec.SectionName LIKE @Search
                   OR CAST(se.RollNo AS nvarchar(20)) LIKE @Search)
            ORDER BY ay.YearName DESC, st.StudentName, se.EnrollmentDate DESC;
            """;

        var list = new List<StudentEnrollmentLinkListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentEnrollmentLinkListItem
            {
                StudentEnrollmentId = Convert.ToInt32(reader["UID"]),
                YearName = reader["YearName"] as string ?? string.Empty,
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                StudentName = reader["StudentName"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                ClassSectionDisplay = reader["ClassSectionDisplay"] as string,
                RollNo = reader["RollNo"] is DBNull ? null : Convert.ToInt32(reader["RollNo"]),
                EnrollmentDate = Convert.ToDateTime(reader["EnrollmentDate"])
            });
        }

        return list;
    }

    public async Task<StudentEnrollmentLinkFormModel?> GetAsync(int studentEnrollmentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT UID, AcademicYearID, StudentID, ProgramID, ClassSectionID, RollNo, EnrollmentDate
            FROM dbo.StudentEnrollments
            WHERE UID = @StudentEnrollmentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentEnrollmentId", studentEnrollmentId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new StudentEnrollmentLinkFormModel
        {
            StudentEnrollmentId = Convert.ToInt32(reader["UID"]),
            AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
            StudentId = Convert.ToInt32(reader["StudentID"]),
            ProgramId = Convert.ToInt32(reader["ProgramID"]),
            ClassSectionId = reader["ClassSectionID"] is DBNull ? null : Convert.ToInt32(reader["ClassSectionID"]),
            RollNo = reader["RollNo"] is DBNull ? null : Convert.ToInt32(reader["RollNo"]),
            EnrollmentDate = Convert.ToDateTime(reader["EnrollmentDate"])
        };
    }

    public async Task<StudentEnrollmentLinkLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string yearsSql = """
            SELECT AcademicYearID, YearName
            FROM dbo.AcademicYears
            ORDER BY YearName DESC;
            """;

        const string studentsSql = """
            SELECT StudentID, ISNULL(RegistrationNo + N' · ', N'') + StudentName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY StudentName;
            """;

        const string programsSql = """
            SELECT ProgramID, ProgramName
            FROM dbo.Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
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

        return new StudentEnrollmentLinkLookups
        {
            AcademicYears = await ReadLookupAsync(connection, yearsSql, cancellationToken),
            Students = await ReadLookupAsync(connection, studentsSql, cancellationToken),
            Programs = await ReadLookupAsync(connection, programsSql, cancellationToken),
            ClassSections = await ReadLookupAsync(connection, classSectionsSql, cancellationToken)
        };
    }

    public async Task<int> InsertAsync(StudentEnrollmentLinkFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.StudentEnrollments
                (AcademicYearID, StudentID, ProgramID, ClassSectionID, RollNo, EnrollmentDate)
            VALUES
                (@AcademicYearId, @StudentId, @ProgramId, @ClassSectionId, @RollNo, @EnrollmentDate);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(StudentEnrollmentLinkFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentEnrollments SET
                AcademicYearID = @AcademicYearId,
                StudentID = @StudentId,
                ProgramID = @ProgramId,
                ClassSectionID = @ClassSectionId,
                RollNo = @RollNo,
                EnrollmentDate = @EnrollmentDate
            WHERE UID = @StudentEnrollmentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentEnrollmentId", model.StudentEnrollmentId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int studentEnrollmentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.StudentEnrollments WHERE UID = @StudentEnrollmentId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentEnrollmentId", studentEnrollmentId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, StudentEnrollmentLinkFormModel model)
    {
        command.Parameters.AddWithValue("@AcademicYearId", model.AcademicYearId);
        command.Parameters.AddWithValue("@StudentId", model.StudentId);
        command.Parameters.AddWithValue("@ProgramId", model.ProgramId);
        command.Parameters.AddWithValue("@ClassSectionId", (object?)model.ClassSectionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@RollNo", (object?)model.RollNo ?? DBNull.Value);
        command.Parameters.AddWithValue("@EnrollmentDate", model.EnrollmentDate.Date);
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
