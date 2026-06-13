using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeeLookupRepository : IFeeLookupRepository
{
    private readonly string _connectionString;

    public FeeLookupRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetProgramsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ProgramID, ProgramCode + ' - ' + ProgramName
            FROM dbo.Programs WHERE IsActive = 1 ORDER BY ProgramName;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeClassLookupItem>> GetClassesByProgramAsync(
        int programId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ClassID, ClassCode + N' · ' + ClassName AS DisplayName
            FROM dbo.Classes
            WHERE IsActive = 1
            ORDER BY SortOrder, ClassCode;
            """;

        var list = new List<FeeClassLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeClassLookupItem
            {
                Id = FeeSql.ToInt32(reader, "ClassID"),
                Name = reader["DisplayName"] as string ?? "",
                Semester = string.Empty,
                AcademicYear = 0
            });
        }

        return list;
    }

    public async Task<ProgramBulkChallanContext?> ResolveProgramBulkChallanContextAsync(
        int programId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                fs.Uid,
                fs.StructureName,
                fs.Semester,
                fs.AcademicYear
            FROM dbo.FeeStructures fs
            WHERE fs.IsActive = 1
              AND fs.ProgramID = @ProgramId
              AND EXISTS (SELECT 1 FROM dbo.FeeStructureDetails fsd WHERE fsd.StructureID = fs.Uid)
            ORDER BY fs.AcademicYear DESC, fs.Uid DESC;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var semester = reader["Semester"] as string ?? string.Empty;
        var academicYear = FeeSql.ToInt16(reader, "AcademicYear");
        var structureName = reader["StructureName"] as string ?? string.Empty;
        return new ProgramBulkChallanContext
        {
            StructureId = FeeSql.ToInt32(reader, "Uid"),
            StructureName = structureName,
            Semester = semester,
            AcademicYear = academicYear
        };
    }

    public async Task<IReadOnlyList<ProgramBulkChallanContext>> GetProgramStructuresAsync(
        int programId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                fs.Uid,
                fs.StructureName,
                fs.Semester,
                fs.AcademicYear
            FROM dbo.FeeStructures fs
            WHERE fs.IsActive = 1
              AND fs.ProgramID = @ProgramId
              AND EXISTS (SELECT 1 FROM dbo.FeeStructureDetails fsd WHERE fsd.StructureID = fs.Uid)
            ORDER BY fs.AcademicYear DESC, fs.StructureName;
            """;

        var list = new List<ProgramBulkChallanContext>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ProgramBulkChallanContext
            {
                StructureId = FeeSql.ToInt32(reader, "Uid"),
                StructureName = reader["StructureName"] as string ?? string.Empty,
                Semester = reader["Semester"] as string ?? string.Empty,
                AcademicYear = FeeSql.ToInt16(reader, "AcademicYear")
            });
        }

        return list;
    }

    public async Task<ProgramBulkChallanContext?> ResolveStructureBulkChallanContextAsync(
        int programId,
        int structureId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                fs.Uid,
                fs.StructureName,
                fs.Semester,
                fs.AcademicYear
            FROM dbo.FeeStructures fs
            WHERE fs.IsActive = 1
              AND fs.Uid = @StructureId
              AND fs.ProgramID = @ProgramId
              AND EXISTS (SELECT 1 FROM dbo.FeeStructureDetails fsd WHERE fsd.StructureID = fs.Uid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@StructureId", structureId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ProgramBulkChallanContext
        {
            StructureId = FeeSql.ToInt32(reader, "Uid"),
            StructureName = reader["StructureName"] as string ?? string.Empty,
            Semester = reader["Semester"] as string ?? string.Empty,
            AcademicYear = FeeSql.ToInt16(reader, "AcademicYear")
        };
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetActiveStudentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT StudentID, RegistrationNo + ' - ' + StudentName
            FROM dbo.Students WHERE IsActive = 1 ORDER BY RegistrationNo;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetActiveFeeHeadsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, HeadCode + ' - ' + HeadName
            FROM dbo.ref_FeeHeads WHERE IsActive = 1 ORDER BY HeadName;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetActiveStructuresAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT fs.Uid,
                   fs.StructureName + ' (' + p.ProgramName + ', ' + fs.Semester + ' ' + CAST(fs.AcademicYear AS varchar(4)) + ')'
            FROM dbo.FeeStructures fs
            INNER JOIN dbo.Programs p ON fs.ProgramID = p.ProgramID
            WHERE fs.IsActive = 1
            ORDER BY fs.AcademicYear DESC, fs.StructureName;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetActiveStructuresByProgramAsync(
        int programId,
        string? semester = null,
        short? academicYear = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT fs.Uid,
                   fs.StructureName + ' (' + fs.Semester + ' ' + CAST(fs.AcademicYear AS varchar(4)) + ')'
            FROM dbo.FeeStructures fs
            WHERE fs.IsActive = 1
              AND fs.ProgramID = @ProgramId
              AND (@Semester IS NULL OR fs.Semester = @Semester)
              AND (@AcademicYear IS NULL OR fs.AcademicYear = @AcademicYear)
            ORDER BY fs.AcademicYear DESC, fs.StructureName;
            """;
        var list = new List<FeeLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@Semester", string.IsNullOrWhiteSpace(semester) ? DBNull.Value : semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", academicYear.HasValue ? academicYear.Value : DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeLookupItem
            {
                Id = FeeSql.ToInt32(reader, 0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetUnpaidChallansAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid,
                   c.ChallanNo + ' - '
                   + COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                              NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), ''))
                   + ' [' + COALESCE(s.RegistrationNo, a.ApplicationNo, '') + ']'
                   + ' (Bal: ' + CAST(c.NetPayable - c.AmountPaid AS varchar(20)) + ')'
            FROM dbo.Challans c
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            WHERE c.IsActive = 1
              AND c.Status NOT IN ('Paid', 'Cancelled')
              AND c.AmountPaid < c.NetPayable
            ORDER BY c.DueDate;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    private async Task<IReadOnlyList<FeeLookupItem>> ReadLookupAsync(string sql, CancellationToken cancellationToken)
    {
        var list = new List<FeeLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeLookupItem
            {
                Id = FeeSql.ToInt32(reader, 0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }
}
