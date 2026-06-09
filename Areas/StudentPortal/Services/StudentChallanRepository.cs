using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentChallanRepository : IStudentChallanRepository
{
    private readonly string _connectionString;

    public StudentChallanRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<StudentChallanPageModel> GetCurrentMonthChallanAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            SELECT TOP (1)
                c.Uid,
                c.ChallanNo,
                c.Semester,
                c.AcademicYear,
                c.IssueDate,
                c.DueDate,
                c.NetPayable,
                c.AmountPaid,
                c.Status
            FROM dbo.Challans c
            WHERE c.StudentID = @StudentUid
              AND c.IsActive = 1
              AND YEAR(c.IssueDate) = YEAR(SYSUTCDATETIME())
              AND MONTH(c.IssueDate) = MONTH(SYSUTCDATETIME())
            ORDER BY c.IssueDate DESC, c.Uid DESC;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        StudentChallanSummary? header;
        await using (var command = new SqlCommand(headerSql, connection))
        {
            command.Parameters.AddWithValue("@StudentUid", studentUid);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new StudentChallanPageModel();
            }

            header = MapSummary(reader);
        }

        var lines = await LoadLinesAsync(connection, header.Uid, cancellationToken);
        return new StudentChallanPageModel
        {
            Challan = header,
            Lines = lines
        };
    }

    public async Task<IReadOnlyList<StudentChallanSummary>> ListChallanHistoryAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.Uid,
                c.ChallanNo,
                c.Semester,
                c.AcademicYear,
                c.IssueDate,
                c.DueDate,
                c.NetPayable,
                c.AmountPaid,
                c.Status
            FROM dbo.Challans c
            WHERE c.StudentID = @StudentUid
              AND c.IsActive = 1
            ORDER BY c.IssueDate DESC, c.Uid DESC;
            """;

        var list = new List<StudentChallanSummary>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapSummary(reader));
        }

        return list;
    }

    private static async Task<IReadOnlyList<StudentChallanLine>> LoadLinesAsync(
        SqlConnection connection,
        int challanUid,
        CancellationToken cancellationToken)
    {
        const string linesSql = """
            SELECT
                fh.HeadName,
                cd.Amount,
                cd.DiscountAmount,
                cd.LateFine,
                cd.NetAmount
            FROM dbo.ChallanDetails cd
            INNER JOIN dbo.ref_FeeHeads fh ON cd.FeeHeadID = fh.Uid
            WHERE cd.ChallanID = @ChallanUid
            ORDER BY fh.HeadName;
            """;

        var lines = new List<StudentChallanLine>();
        await using var command = new SqlCommand(linesSql, connection);
        command.Parameters.AddWithValue("@ChallanUid", challanUid);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(new StudentChallanLine
            {
                FeeHeadName = reader["HeadName"] as string ?? string.Empty,
                Amount = Convert.ToDecimal(reader["Amount"]),
                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),
                LateFine = Convert.ToDecimal(reader["LateFine"]),
                NetAmount = Convert.ToDecimal(reader["NetAmount"])
            });
        }

        return lines;
    }

    private static StudentChallanSummary MapSummary(SqlDataReader reader)
    {
        var dueDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("DueDate")));
        var net = Convert.ToDecimal(reader["NetPayable"]);
        var paid = Convert.ToDecimal(reader["AmountPaid"]);
        var stored = reader["Status"] as string ?? "Unpaid";

        return new StudentChallanSummary
        {
            Uid = Convert.ToInt32(reader["Uid"]),
            ChallanNo = reader["ChallanNo"] as string ?? string.Empty,
            Semester = reader["Semester"] as string ?? string.Empty,
            AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
            IssueDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("IssueDate"))),
            DueDate = dueDate,
            NetPayable = net,
            AmountPaid = paid,
            Status = stored,
            DisplayStatus = ComputeDisplayStatus(net, paid, dueDate, stored)
        };
    }

    private static string ComputeDisplayStatus(decimal netPayable, decimal amountPaid, DateOnly dueDate, string storedStatus)
    {
        if (string.Equals(storedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (amountPaid <= 0)
        {
            return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : "Unpaid";
        }

        if (amountPaid >= netPayable)
        {
            return "Paid";
        }

        return dueDate < DateOnly.FromDateTime(DateTime.Today) ? "Overdue" : "Partial";
    }
}
