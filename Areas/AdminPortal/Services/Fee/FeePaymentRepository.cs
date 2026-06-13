using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeePaymentRepository : IFeePaymentRepository
{
    private readonly string _connectionString;
    private readonly IFeeChallanRepository _challans;

    public FeePaymentRepository(IConfiguration configuration, IFeeChallanRepository challans)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _challans = challans;
    }

    public async Task<IReadOnlyList<PaymentListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.Uid, p.ChallanID, c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   p.AmountPaid, p.PaymentDate, p.PaymentMode, p.Status, pr.ReceiptNo
            FROM dbo.Payments p
            INNER JOIN dbo.Challans c ON p.ChallanID = c.Uid
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            LEFT JOIN dbo.PaymentReceipts pr ON pr.PaymentID = p.Uid
            WHERE p.IsActive = 1
            ORDER BY p.PaymentDate DESC, p.Uid DESC;
            """;

        return await ReadPaymentListAsync(sql, null, cancellationToken);
    }

    public async Task<PaymentFormModel?> GetChallanForPaymentAsync(int challanId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid, c.ChallanNo, c.ApplicationUid,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   COALESCE(s.RegistrationNo, a.ApplicationNo) AS PartyNo,
                   c.NetPayable, c.AmountPaid
            FROM dbo.Challans c
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            WHERE c.Uid = @Uid AND c.IsActive = 1 AND c.Status <> 'Cancelled';
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", challanId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var isApplicationChallan = reader["ApplicationUid"] is not DBNull;
        var partyNo = reader["PartyNo"] as string;
        var netPayable = FeeSql.ToDecimal(reader, "NetPayable");
        var amountPaidSoFar = FeeSql.ToDecimal(reader, "AmountPaid");

        return new PaymentFormModel
        {
            ChallanId = FeeSql.ToInt32(reader, "Uid"),
            ChallanNo = reader["ChallanNo"] as string,
            StudentName = reader["StudentName"] as string,
            ApplicantId = isApplicationChallan ? partyNo : null,
            IsApplicationChallan = isApplicationChallan,
            NetPayable = netPayable,
            AmountPaidSoFar = amountPaidSoFar,
            AmountPaid = Math.Max(0, netPayable - amountPaidSoFar),
            PaymentDate = DateOnly.FromDateTime(DateTime.Today)
        };
    }

    public async Task<int> RecordPaymentAsync(PaymentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        if (model.AmountPaid <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertPayment = """
                INSERT INTO dbo.Payments
                    (ChallanID, AmountPaid, PaymentDate, PaymentMode, TransactionRef, BankName, BranchName,
                     ChequeNo, ChequeDate, Status, Remarks, IsActive, CreatedBy, CreatedAt)
                VALUES
                    (@ChallanId, @AmountPaid, @PaymentDate, @PaymentMode, @TransactionRef, @BankName, @BranchName,
                     @ChequeNo, @ChequeDate, @Status, @Remarks, 1, @CreatedBy, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;

            int paymentId;
            await using (var command = new SqlCommand(insertPayment, connection, transaction))
            {
                command.Parameters.AddWithValue("@ChallanId", model.ChallanId);
                command.Parameters.AddWithValue("@AmountPaid", model.AmountPaid);
                command.Parameters.AddWithValue("@PaymentDate", model.PaymentDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@PaymentMode", model.PaymentMode.Trim());
                command.Parameters.AddWithValue("@TransactionRef", (object?)model.TransactionRef?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@BankName", (object?)model.BankName?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchName", (object?)model.BranchName?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@ChequeNo", (object?)model.ChequeNo?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@ChequeDate", model.ChequeDate.HasValue ? model.ChequeDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
                command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", FeeConstants.PaymentStatusVerified);
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                paymentId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            const string updateChallan = """
                UPDATE dbo.Challans SET
                    AmountPaid = AmountPaid + @AmountPaid,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE Uid = @ChallanId;
                """;
            await using (var command = new SqlCommand(updateChallan, connection, transaction))
            {
                command.Parameters.AddWithValue("@ChallanId", model.ChallanId);
                command.Parameters.AddWithValue("@AmountPaid", model.AmountPaid);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var receiptNo = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{paymentId:D5}";
            const string insertReceipt = """
                INSERT INTO dbo.PaymentReceipts
                    (PaymentID, ReceiptNo, IssuedAt, IssuedBy, PrintCount, CreatedBy, CreatedAt)
                VALUES
                    (@PaymentId, @ReceiptNo, SYSUTCDATETIME(), @IssuedBy, 0, @CreatedBy, SYSUTCDATETIME());
                """;
            await using (var command = new SqlCommand(insertReceipt, connection, transaction))
            {
                command.Parameters.AddWithValue("@PaymentId", paymentId);
                command.Parameters.AddWithValue("@ReceiptNo", receiptNo);
                command.Parameters.AddWithValue("@IssuedBy", DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            await _challans.RecalculateStatusAsync(model.ChallanId, cancellationToken);
            return paymentId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentReceiptViewModel?> GetReceiptAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.Uid AS PaymentId, pr.ReceiptNo, pr.IssuedAt, pr.IssuedBy,
                   c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   COALESCE(s.RegistrationNo, a.ApplicationNo) AS RegistrationNo,
                   p.AmountPaid, p.PaymentMode, p.PaymentDate, p.TransactionRef,
                   c.NetPayable, c.AmountPaid AS ChallanTotalPaid
            FROM dbo.Payments p
            INNER JOIN dbo.PaymentReceipts pr ON pr.PaymentID = p.Uid
            INNER JOIN dbo.Challans c ON p.ChallanID = c.Uid
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            WHERE p.Uid = @PaymentId;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PaymentId", paymentId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PaymentReceiptViewModel
        {
            PaymentId = FeeSql.ToInt32(reader, "PaymentId"),
            ReceiptNo = reader["ReceiptNo"] as string ?? "",
            IssuedAt = reader.GetDateTime(reader.GetOrdinal("IssuedAt")),
            IssuedBy = reader["IssuedBy"] as string,
            ChallanNo = reader["ChallanNo"] as string ?? "",
            StudentName = reader["StudentName"] as string ?? "",
            RegistrationNo = reader["RegistrationNo"] as string ?? "",
            AmountPaid = FeeSql.ToDecimal(reader, "AmountPaid"),
            PaymentMode = reader["PaymentMode"] as string ?? "",
            PaymentDate = FeeSql.ToDateOnly(reader, "PaymentDate"),
            TransactionRef = reader["TransactionRef"] as string,
            ChallanNetPayable = FeeSql.ToDecimal(reader, "NetPayable"),
            ChallanTotalPaid = FeeSql.ToDecimal(reader, "ChallanTotalPaid")
        };
    }

    public async Task IncrementPrintCountAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.PaymentReceipts SET
                PrintCount = PrintCount + 1,
                LastPrintedAt = SYSUTCDATETIME()
            WHERE PaymentID = @PaymentId;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PaymentId", paymentId);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentListItem>> ListReceiptsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.Uid, p.ChallanID, c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   p.AmountPaid, p.PaymentDate, p.PaymentMode, p.Status, pr.ReceiptNo
            FROM dbo.PaymentReceipts pr
            INNER JOIN dbo.Payments p ON pr.PaymentID = p.Uid
            INNER JOIN dbo.Challans c ON p.ChallanID = c.Uid
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            WHERE p.IsActive = 1
            ORDER BY pr.IssuedAt DESC;
            """;
        return await ReadPaymentListAsync(sql, null, cancellationToken);
    }

    private async Task<IReadOnlyList<PaymentListItem>> ReadPaymentListAsync(
        string sql,
        SqlParameter? extra,
        CancellationToken cancellationToken)
    {
        var list = new List<PaymentListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        if (extra is not null)
        {
            command.Parameters.Add(extra);
        }

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PaymentListItem
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

        return list;
    }
}
