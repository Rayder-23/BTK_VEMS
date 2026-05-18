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
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs WHERE IsActive = 1 ORDER BY ProgramName;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetActiveStudentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, RegistrationNo + ' - ' + FirstName + ' ' + LastName
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
            INNER JOIN dbo.ref_Programs p ON fs.ProgramID = p.Uid
            WHERE fs.IsActive = 1
            ORDER BY fs.AcademicYear DESC, fs.StructureName;
            """;
        return await ReadLookupAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeLookupItem>> GetUnpaidChallansAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid,
                   c.ChallanNo + ' - ' + s.FirstName + ' ' + s.LastName + ' (Bal: ' + CAST(c.NetPayable - c.AmountPaid AS varchar(20)) + ')'
            FROM dbo.Challans c
            INNER JOIN dbo.Students s ON c.StudentID = s.Uid
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
