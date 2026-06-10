using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ProgramEnrollmentRepository : IProgramEnrollmentRepository
{
    public static readonly IReadOnlyList<string> AllowedEnrollmentStatuses =
    ["Active", "Passed", "Failed", "Withdrawn", "Transferred", "Expelled"];

    public static readonly IReadOnlyList<string> AllowedFeeStatuses =
    ["Pending", "Paid", "Partial", "Waived"];

    private const string EnrollmentStatusConfigKey = "EnrollmentStatus";
    private const string FeeStatusConfigKey = "FeeStatus";

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
                s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName AS StudentName,
                p.ProgramName,
                se.AcademicYear,
                se.GradeOrSemester,
                se.RollNo,
                se.EnrollmentStatus,
                se.FeeStatus
            FROM dbo.StudentEnrollments se
            INNER JOIN dbo.Students s ON se.StudentID = s.Uid
            INNER JOIN dbo.ref_Programs p ON se.ProgramID = p.Uid
            WHERE (@Search IS NULL
                   OR s.RegistrationNo LIKE @Search
                   OR s.FirstName LIKE @Search
                   OR s.LastName LIKE @Search
                   OR se.RollNo LIKE @Search
                   OR p.ProgramName LIKE @Search)
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
                AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
                GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
                RollNo = reader["RollNo"] as string ?? string.Empty,
                EnrollmentStatus = reader["EnrollmentStatus"] as string ?? string.Empty,
                FeeStatus = reader["FeeStatus"] as string ?? string.Empty
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
                AcademicYear,
                GradeOrSemester,
                SectionID,
                RollNo,
                EnrollmentDate,
                CompletionDate,
                EnrollmentStatus,
                FeeStatus,
                Remarks
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
            SELECT Uid, RegistrationNo + ' - ' + FirstName + ' ' + LastName
            FROM dbo.Students
            WHERE IsActive = 1
            ORDER BY RegistrationNo;
            """;

        const string programsSql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var sectionsSql = """
            SELECT Uid, SectionName + ' · ' + GradeLevel + ' (' + AcademicYear + ')'
            FROM dbo.Sections
            WHERE IsActive = 1
            """ + (programId is > 0 ? " AND ProgramId = @ProgramId" : "") + """
             ORDER BY SectionName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var students = await ReadLookupAsync(connection, studentsSql, null, cancellationToken);
        var programs = await ReadLookupAsync(connection, programsSql, null, cancellationToken);
        var sections = await ReadLookupAsync(
            connection,
            sectionsSql,
            programId is > 0 ? new SqlParameter("@ProgramId", programId.Value) : null,
            cancellationToken);

        if (sections.Count == 0)
        {
            sections =
            [
                new StudentLookupItem { Id = 1, Name = "Section 1 (default)" },
                new StudentLookupItem { Id = 2, Name = "Section 2 (default)" }
            ];
        }

        var enrollmentStatuses = ResolveConfiguredValues(
            await _configurations.GetValuesAsync(EnrollmentStatusConfigKey, cancellationToken),
            AllowedEnrollmentStatuses);
        var feeStatuses = ResolveConfiguredValues(
            await _configurations.GetValuesAsync(FeeStatusConfigKey, cancellationToken),
            AllowedFeeStatuses);

        return new ProgramEnrollmentLookups
        {
            Students = students,
            Programs = programs,
            Sections = sections,
            EnrollmentStatuses = enrollmentStatuses,
            FeeStatuses = feeStatuses
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

    public async Task<bool> RollNoExistsAsync(
        int programId,
        short academicYear,
        byte gradeOrSemester,
        string rollNo,
        int? excludeUid,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentEnrollments
            WHERE ProgramID = @ProgramID
              AND AcademicYear = @AcademicYear
              AND GradeOrSemester = @GradeOrSemester
              AND RollNo = @RollNo
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramID", programId);
        command.Parameters.AddWithValue("@AcademicYear", academicYear);
        command.Parameters.AddWithValue("@GradeOrSemester", gradeOrSemester);
        command.Parameters.AddWithValue("@RollNo", rollNo.Trim());
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
                AcademicYear,
                GradeOrSemester,
                SectionID,
                RollNo,
                EnrollmentDate,
                CompletionDate,
                EnrollmentStatus,
                FeeStatus,
                Remarks,
                CreatedBy
            )
            VALUES (
                @StudentID,
                @ProgramID,
                @AcademicYear,
                @GradeOrSemester,
                @SectionID,
                @RollNo,
                @EnrollmentDate,
                @CompletionDate,
                @EnrollmentStatus,
                @FeeStatus,
                @Remarks,
                @CreatedBy
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ProgramEnrollmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentEnrollments SET
                StudentID = @StudentID,
                ProgramID = @ProgramID,
                AcademicYear = @AcademicYear,
                GradeOrSemester = @GradeOrSemester,
                SectionID = @SectionID,
                RollNo = @RollNo,
                EnrollmentDate = @EnrollmentDate,
                CompletionDate = @CompletionDate,
                EnrollmentStatus = @EnrollmentStatus,
                FeeStatus = @FeeStatus,
                Remarks = @Remarks,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> WithdrawAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentEnrollments SET
                EnrollmentStatus = 'Withdrawn',
                CompletionDate = COALESCE(CompletionDate, CONVERT(date, SYSUTCDATETIME())),
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid
              AND EnrollmentStatus <> 'Withdrawn';
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ProgramEnrollmentFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        StudentId = Convert.ToInt32(reader["StudentID"]),
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        GradeOrSemester = Convert.ToByte(reader["GradeOrSemester"]),
        SectionId = Convert.ToInt32(reader["SectionID"]),
        RollNo = reader["RollNo"] as string ?? string.Empty,
        EnrollmentDate = reader.GetDateTime(reader.GetOrdinal("EnrollmentDate")),
        CompletionDate = reader["CompletionDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("CompletionDate")),
        EnrollmentStatus = reader["EnrollmentStatus"] as string ?? "Active",
        FeeStatus = reader["FeeStatus"] as string ?? "Pending",
        Remarks = reader["Remarks"] as string
    };

    private static void Bind(SqlCommand command, ProgramEnrollmentFormModel model)
    {
        command.Parameters.AddWithValue("@StudentID", model.StudentId);
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@GradeOrSemester", model.GradeOrSemester);
        command.Parameters.AddWithValue("@SectionID", model.SectionId);
        command.Parameters.AddWithValue("@RollNo", model.RollNo.Trim());
        command.Parameters.AddWithValue("@EnrollmentDate", model.EnrollmentDate!.Value.Date);
        command.Parameters.AddWithValue("@CompletionDate", (object?)model.CompletionDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@EnrollmentStatus", model.EnrollmentStatus.Trim());
        command.Parameters.AddWithValue("@FeeStatus", model.FeeStatus.Trim());
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
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
