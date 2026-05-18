using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeeChallanRepository : IFeeChallanRepository
{
    private readonly string _connectionString;
    private readonly IFeeStructureRepository _structures;
    private readonly IFeeConcessionRepository _concessions;

    public FeeChallanRepository(
        IConfiguration configuration,
        IFeeStructureRepository structures,
        IFeeConcessionRepository concessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _structures = structures;
        _concessions = concessions;
    }

    public async Task<IReadOnlyList<ChallanListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid, c.ChallanNo, s.FirstName + ' ' + s.LastName AS StudentName, s.RegistrationNo,
                   c.Semester, c.AcademicYear, c.DueDate, c.NetPayable, c.AmountPaid, c.Status
            FROM dbo.Challans c
            INNER JOIN dbo.Students s ON c.StudentID = s.Uid
            WHERE c.IsActive = 1
              AND (@Search IS NULL OR c.ChallanNo LIKE @Search OR s.RegistrationNo LIKE @Search
                   OR s.FirstName LIKE @Search OR s.LastName LIKE @Search)
            ORDER BY c.Uid DESC;
            """;

        var list = new List<ChallanListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dueDate = FeeSql.ToDateOnly(reader, "DueDate");
            var net = FeeSql.ToDecimal(reader, "NetPayable");
            var paid = FeeSql.ToDecimal(reader, "AmountPaid");
            var stored = reader["Status"] as string ?? "Unpaid";
            list.Add(new ChallanListItem
            {
                Uid = FeeSql.ToInt32(reader, "Uid"),
                ChallanNo = reader["ChallanNo"] as string ?? "",
                StudentName = reader["StudentName"] as string ?? "",
                RegistrationNo = reader["RegistrationNo"] as string ?? "",
                Semester = reader["Semester"] as string ?? "",
                AcademicYear = FeeSql.ToInt16(reader, "AcademicYear"),
                DueDate = dueDate,
                NetPayable = net,
                AmountPaid = paid,
                Status = stored,
                DisplayStatus = FeeStatusHelper.ComputeChallanStatus(net, paid, dueDate, stored)
            });
        }

        return list;
    }

    public async Task<ChallanDetailsPageModel?> GetDetailsAsync(int challanId, CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            SELECT c.Uid, c.ChallanNo, s.FirstName + ' ' + s.LastName AS StudentName, s.RegistrationNo,
                   c.Semester, c.AcademicYear, c.DueDate, c.NetPayable, c.AmountPaid, c.Status
            FROM dbo.Challans c
            INNER JOIN dbo.Students s ON c.StudentID = s.Uid
            WHERE c.Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        ChallanListItem? header = null;
        await using (var command = new SqlCommand(headerSql, connection))
        {
            command.Parameters.AddWithValue("@Uid", challanId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var dueDate = FeeSql.ToDateOnly(reader, "DueDate");
            var net = FeeSql.ToDecimal(reader, "NetPayable");
            var paid = FeeSql.ToDecimal(reader, "AmountPaid");
            var stored = reader["Status"] as string ?? "Unpaid";
            header = new ChallanListItem
            {
                Uid = FeeSql.ToInt32(reader, "Uid"),
                ChallanNo = reader["ChallanNo"] as string ?? "",
                StudentName = reader["StudentName"] as string ?? "",
                RegistrationNo = reader["RegistrationNo"] as string ?? "",
                Semester = reader["Semester"] as string ?? "",
                AcademicYear = FeeSql.ToInt16(reader, "AcademicYear"),
                DueDate = dueDate,
                NetPayable = net,
                AmountPaid = paid,
                Status = stored,
                DisplayStatus = FeeStatusHelper.ComputeChallanStatus(net, paid, dueDate, stored)
            };
        }

        const string linesSql = """
            SELECT cd.Uid, fh.HeadName, cd.Amount, cd.DiscountAmount, cd.LateFine, cd.NetAmount
            FROM dbo.ChallanDetails cd
            INNER JOIN dbo.ref_FeeHeads fh ON cd.FeeHeadID = fh.Uid
            WHERE cd.ChallanID = @ChallanId
            ORDER BY fh.HeadName;
            """;

        var lines = new List<ChallanDetailLine>();
        await using (var command = new SqlCommand(linesSql, connection))
        {
            command.Parameters.AddWithValue("@ChallanId", challanId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                lines.Add(new ChallanDetailLine
                {
                    Uid = FeeSql.ToInt32(reader, "Uid"),
                    FeeHeadName = reader["HeadName"] as string ?? "",
                    Amount = FeeSql.ToDecimal(reader, "Amount"),
                    DiscountAmount = FeeSql.ToDecimal(reader, "DiscountAmount"),
                    LateFine = FeeSql.ToDecimal(reader, "LateFine"),
                    NetAmount = FeeSql.ToDecimal(reader, "NetAmount")
                });
            }
        }

        const string paymentsSql = """
            SELECT p.Uid, p.ChallanID, c.ChallanNo, s.FirstName + ' ' + s.LastName AS StudentName,
                   p.AmountPaid, p.PaymentDate, p.PaymentMode, p.Status, pr.ReceiptNo
            FROM dbo.Payments p
            INNER JOIN dbo.Challans c ON p.ChallanID = c.Uid
            INNER JOIN dbo.Students s ON c.StudentID = s.Uid
            LEFT JOIN dbo.PaymentReceipts pr ON pr.PaymentID = p.Uid
            WHERE p.ChallanID = @ChallanId AND p.IsActive = 1
            ORDER BY p.PaymentDate DESC, p.Uid DESC;
            """;

        var payments = new List<PaymentListItem>();
        await using (var command = new SqlCommand(paymentsSql, connection))
        {
            command.Parameters.AddWithValue("@ChallanId", challanId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                payments.Add(new PaymentListItem
                {
                    Uid = FeeSql.ToInt32(reader, "Uid"),
                    ChallanId = FeeSql.ToInt32(reader, "ChallanID"),
                    ChallanNo = reader["ChallanNo"] as string ?? "",
                    StudentName = reader["StudentName"] as string ?? "",
                    AmountPaid = FeeSql.ToDecimal(reader, "AmountPaid"),
                    PaymentDate = FeeSql.ToDateOnly(reader, "PaymentDate"),
                    PaymentMode = reader["PaymentMode"] as string ?? "",
                    Status = reader["Status"] as string ?? "",
                    ReceiptNo = reader["ReceiptNo"] as string
                });
            }
        }

        return new ChallanDetailsPageModel
        {
            Header = header!,
            Lines = lines,
            Payments = payments
        };
    }

    public async Task<int> GenerateChallanAsync(ChallanGenerateFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        var structure = await _structures.GetAsync(model.StructureId, cancellationToken)
            ?? throw new InvalidOperationException("Fee structure not found.");

        if (!structure.IsActive)
        {
            throw new InvalidOperationException("Fee structure is inactive.");
        }

        var details = await _structures.GetDetailsForStructureAsync(model.StructureId, cancellationToken);
        if (details.Count == 0)
        {
            throw new InvalidOperationException("Fee structure has no line items. Add structure details first.");
        }

        decimal totalAmount = 0;
        decimal lineDiscountTotal = 0;
        var linePayloads = new List<(short FeeHeadId, decimal Amount, decimal Discount, decimal LateFine, decimal Net)>();

        foreach (var line in details)
        {
            var concessionDiscount = await _concessions.GetApplicableDiscountForHeadAsync(
                model.StudentId, line.FeeHeadId, line.Amount, model.IssueDate, cancellationToken);
            var net = line.Amount - concessionDiscount;
            totalAmount += line.Amount;
            lineDiscountTotal += concessionDiscount;
            linePayloads.Add((line.FeeHeadId, line.Amount, concessionDiscount, 0, net));
        }

        var extraDiscount = Math.Max(0, model.DiscountAmount);
        var discountAmount = lineDiscountTotal + extraDiscount;
        var netPayable = totalAmount - discountAmount;

        if (netPayable < 0)
        {
            netPayable = 0;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var challanNo = await AllocateChallanNoAsync(connection, transaction, cancellationToken);
            const string insertChallan = """
                INSERT INTO dbo.Challans
                    (ChallanNo, StudentID, StructureID, Semester, AcademicYear, IssueDate, DueDate,
                     TotalAmount, DiscountAmount, LateFineAmount, NetPayable, AmountPaid, Status, Remarks,
                     IsActive, CreatedBy, CreatedAt)
                VALUES
                    (@ChallanNo, @StudentId, @StructureId, @Semester, @AcademicYear, @IssueDate, @DueDate,
                     @TotalAmount, @DiscountAmount, 0, @NetPayable, 0, 'Unpaid', @Remarks,
                     1, @CreatedBy, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;

            int challanId;
            await using (var command = new SqlCommand(insertChallan, connection, transaction))
            {
                command.Parameters.AddWithValue("@ChallanNo", challanNo);
                command.Parameters.AddWithValue("@StudentId", model.StudentId);
                command.Parameters.AddWithValue("@StructureId", model.StructureId);
                command.Parameters.AddWithValue("@Semester", structure.Semester);
                command.Parameters.AddWithValue("@AcademicYear", structure.AcademicYear);
                command.Parameters.AddWithValue("@IssueDate", model.IssueDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@DueDate", model.DueDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                command.Parameters.AddWithValue("@NetPayable", netPayable);
                command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                challanId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            const string insertDetail = """
                INSERT INTO dbo.ChallanDetails
                    (ChallanID, FeeHeadID, Amount, DiscountAmount, LateFine, NetAmount, CreatedBy, CreatedAt)
                VALUES
                    (@ChallanId, @FeeHeadId, @Amount, @DiscountAmount, @LateFine, @NetAmount, @CreatedBy, SYSUTCDATETIME());
                """;

            foreach (var line in linePayloads)
            {
                await using var command = new SqlCommand(insertDetail, connection, transaction);
                command.Parameters.AddWithValue("@ChallanId", challanId);
                command.Parameters.AddWithValue("@FeeHeadId", line.FeeHeadId);
                command.Parameters.AddWithValue("@Amount", line.Amount);
                command.Parameters.AddWithValue("@DiscountAmount", line.Discount);
                command.Parameters.AddWithValue("@LateFine", line.LateFine);
                command.Parameters.AddWithValue("@NetAmount", line.Net);
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return challanId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> CancelAsync(int challanId, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Challans SET Status = 'Cancelled', UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid AND Status <> 'Paid';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", challanId);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task RecalculateStatusAsync(int challanId, CancellationToken cancellationToken = default)
    {
        const string readSql = """
            SELECT NetPayable, AmountPaid, DueDate, Status FROM dbo.Challans WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        decimal net;
        decimal paid;
        DateOnly due;
        string current;

        await using (var command = new SqlCommand(readSql, connection))
        {
            command.Parameters.AddWithValue("@Uid", challanId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            net = FeeSql.ToDecimal(reader, "NetPayable");
            paid = FeeSql.ToDecimal(reader, "AmountPaid");
            due = FeeSql.ToDateOnly(reader, "DueDate");
            current = reader["Status"] as string ?? "Unpaid";
        }

        var newStatus = FeeStatusHelper.ResolveStoredStatus(net, paid, due, current);
        if (string.Equals(newStatus, current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        const string updateSql = "UPDATE dbo.Challans SET Status = @Status, UpdatedAt = SYSUTCDATETIME() WHERE Uid = @Uid;";
        await using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.AddWithValue("@Uid", challanId);
            command.Parameters.AddWithValue("@Status", newStatus);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<string> AllocateChallanNoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ISNULL(MAX(CAST(RIGHT(ChallanNo, 4) AS int)), 0) + 1
            FROM dbo.Challans
            WHERE ChallanNo LIKE @Prefix + '%';
            """;
        var prefix = $"CH-{DateTime.UtcNow:yyyyMMdd}-";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Prefix", prefix);
        var seq = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return $"{prefix}{seq:D4}";
    }
}
