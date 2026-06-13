using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ProgramEnrollmentRepository : IProgramEnrollmentRepository
{
    public static readonly IReadOnlyList<string> AllowedEnrollmentStatuses =
    ["Active", "Passed", "Failed", "Withdrawn", "Transferred", "Expelled"];

    private const string EnrollmentStatusConfigKey = "EnrollmentStatus";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public ProgramEnrollmentRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<ProgramEnrollmentListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                se.Uid,
                s.RegistrationNo,
                s.StudentName,
                p.ProgramName,
                c.ClassCode,
                se.AcademicYear,
                se.GradeOrSemester,
                se.RollNo,
                se.EnrollmentStatus,
                se.IsActive
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Students s ON se.StudentID = s.StudentID
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            INNER JOIN dbo.Classes c ON se.ClassID = c.ClassID
            WHERE (@Search IS NULL
                   OR s.RegistrationNo LIKE @Search
                   OR s.StudentName LIKE @Search
                   OR se.RollNo LIKE @Search
                   OR p.ProgramName LIKE @Search
                   OR c.ClassCode LIKE @Search)
            ORDER BY se.AcademicYear DESC, se.GradeOrSemester, se.RollNo;
            """;

        var list = new List<ProgramEnrollmentListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ProgramEnrollmentListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                StudentName = reader["StudentName"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                ClassCode = reader["ClassCode"] as string ?? string.Empty,
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                RollNo = reader["RollNo"] as string ?? string.Empty,
                EnrollmentStatus = reader["EnrollmentStatus"] as string ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<ProgramEnrollmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                StudentID,
                ProgramID,
                ClassID,
                AcademicYear,
                GradeOrSemester,
                RollNo,
                EnrollmentDate,
                EnrollmentStatus,
                IsActive
            FROM dbo.StudentEnrollments
            WHERE Uid = @Uid;
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

        return Map(reader);
    }

    public async Task<ProgramEnrollmentLookups> GetLookupsAsync(
        int? programId,
        CancellationToken cancellationToken = default)
    {
        const string studentsSql = """
            SELECT StudentID, RegistrationNo + ' - ' + StudentName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY RegistrationNo;
            """;

        const string programsSql = """
            SELECT ProgramID, ProgramCode + ' - ' + ProgramName
            FROM dbo.Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var classesSql = """
            SELECT ClassID, ClassCode + ' · ' + ClassName
            FROM dbo.Classes
            WHERE IsActive = 1
             ORDER BY SortOrder, ClassCode;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var students = await ReadLookupAsync(connection, studentsSql, null, cancellationToken);
        var programs = await ReadLookupAsync(connection, programsSql, null, cancellationToken);
        var classes = await ReadLookupAsync(connection, classesSql, null, cancellationToken);

        var enrollmentStatuses = ResolveConfiguredValues(
            await _configurations.GetValuesAsync(EnrollmentStatusConfigKey, cancellationToken),
            AllowedEnrollmentStatuses);

        return new ProgramEnrollmentLookups
        {
            Students = students,
            Programs = programs,
            Classes = classes,
            EnrollmentStatuses = enrollmentStatuses
        };
    }

    public async Task<bool> ExistsForPeriodAsync(
        int studentId,
        int programId,
        short academicYear,
        byte gradeOrSemester,
        int? excludeUid,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentEnrollments
            WHERE StudentID = @StudentID
              AND ProgramID = @ProgramID
              AND AcademicYear = @AcademicYear
              AND GradeOrSemester = @GradeOrSemester
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentID", studentId);
        command.Parameters.AddWithValue("@ProgramID", programId);
        command.Parameters.AddWithValue("@AcademicYear", academicYear);
        command.Parameters.AddWithValue("@GradeOrSemester", gradeOrSemester);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ProgramEnrollmentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.StudentEnrollments (
                StudentID,
                ProgramID,
                ClassID,
                RollNo,
                AcademicYear,
                GradeOrSemester,
                EnrollmentDate,
                EnrollmentStatus,
                IsActive
            )
            VALUES (
                @StudentID,
                @ProgramID,
                @ClassID,
                @RollNo,
                @AcademicYear,
                @GradeOrSemester,
                @EnrollmentDate,
                @EnrollmentStatus,
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

    public async Task<bool> UpdateAsync(ProgramEnrollmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentEnrollments SET
                StudentID = @StudentID,
                ProgramID = @ProgramID,
                ClassID = @ClassID,
                RollNo = @RollNo,
                AcademicYear = @AcademicYear,
                GradeOrSemester = @GradeOrSemester,
                EnrollmentDate = @EnrollmentDate,
                EnrollmentStatus = @EnrollmentStatus,
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

    public async Task<bool> WithdrawAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentEnrollments SET
                EnrollmentStatus = 'Withdrawn',
                IsActive = 0
            WHERE Uid = @Uid
              AND EnrollmentStatus <> 'Withdrawn';
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ProgramEnrollmentFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        StudentId = Convert.ToInt32(reader["StudentID"]),
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        ClassId = Convert.ToInt32(reader["ClassID"]),
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
        RollNo = reader["RollNo"] as string ?? string.Empty,
        EnrollmentDate = reader.GetDateTime(reader.GetOrdinal("EnrollmentDate")),
        EnrollmentStatus = reader["EnrollmentStatus"] as string ?? "Active",
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static void Bind(SqlCommand command, ProgramEnrollmentFormModel model)
    {
        command.Parameters.AddWithValue("@StudentID", model.StudentId);
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@ClassID", model.ClassId);
        command.Parameters.AddWithValue("@RollNo", model.RollNo.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@GradeOrSemester", model.GradeOrSemester);
        command.Parameters.AddWithValue("@EnrollmentDate", model.EnrollmentDate!.Value.Date);
        command.Parameters.AddWithValue("@EnrollmentStatus", model.EnrollmentStatus.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }

    private static IReadOnlyList<string> ResolveConfiguredValues(
        IReadOnlyList<string> configured,
        IReadOnlyList<string> allowed)
    {
        var resolved = configured
            .Select(v => allowed.FirstOrDefault(a => string.Equals(a, v, StringComparison.OrdinalIgnoreCase)))
            .Where(v => v is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return resolved.Count > 0 ? resolved : allowed.ToList();
    }

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupAsync(
        SqlConnection connection,
        string sql,
        SqlParameter? extraParameter,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var command = new SqlCommand(sql, connection);
        if (extraParameter is not null)
        {
            command.Parameters.Add(extraParameter);
        }

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
