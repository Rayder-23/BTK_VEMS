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

    public async Task<IReadOnlyList<ChallanListItem>> ListAsync(string? search, int? programId = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid, c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   COALESCE(s.RegistrationNo, a.ApplicationNo) AS RegistrationNo,
                   c.Semester, c.AcademicYear, c.DueDate, c.NetPayable, c.AmountPaid, c.Status
            FROM dbo.Challans c
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
            LEFT JOIN dbo.FeeStructures fs ON c.StructureID = fs.Uid
            LEFT JOIN dbo.Programs ap ON ap.ProgramCode = a.ProgramCode
            WHERE c.IsActive = 1
              AND (@Search IS NULL OR c.ChallanNo LIKE @Search
                   OR s.RegistrationNo LIKE @Search OR a.ApplicationNo LIKE @Search
                   OR s.StudentName LIKE @Search
                   OR a.FirstName LIKE @Search OR a.LastName LIKE @Search)
              AND (@ProgramId IS NULL
                   OR EXISTS (
                       SELECT 1
                       FROM dbo.StudentEnrollments se
                       WHERE se.StudentID = s.StudentID
                         AND se.ProgramID = @ProgramId
                         AND se.IsActive = 1)
                   OR fs.ProgramID = @ProgramId
                   OR ap.ProgramID = @ProgramId)
            ORDER BY c.Uid DESC;
            """;

        var list = new List<ChallanListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        command.Parameters.AddWithValue("@ProgramId", programId is > 0 ? programId.Value : DBNull.Value);
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
            SELECT c.Uid, c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   COALESCE(s.RegistrationNo, a.ApplicationNo) AS RegistrationNo,
                   c.Semester, c.AcademicYear, c.DueDate, c.NetPayable, c.AmountPaid, c.Status
            FROM dbo.Challans c
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
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
            SELECT p.Uid, p.ChallanID, c.ChallanNo,
                   COALESCE(NULLIF(LTRIM(RTRIM(s.StudentName)), ''),
                            NULLIF(LTRIM(RTRIM(a.FirstName + ' ' + a.LastName)), '')) AS StudentName,
                   p.AmountPaid, p.PaymentDate, p.PaymentMode, p.Status, pr.ReceiptNo
            FROM dbo.Payments p
            INNER JOIN dbo.Challans c ON p.ChallanID = c.Uid
            LEFT JOIN dbo.Students s ON c.StudentID = s.StudentID
            LEFT JOIN dbo.StudentApplications a ON c.ApplicationUid = a.Uid
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
        var isApplicationChallan = model.ApplicationUid.HasValue && model.ApplicationUid.Value > 0;
        if (!isApplicationChallan && model.StudentId <= 0)
        {
            throw new InvalidOperationException("Student is required.");
        }

        var structure = await _structures.GetAsync(model.StructureId, cancellationToken)
            ?? throw new InvalidOperationException("Fee structure not found.");

        if (!structure.IsActive)
        {
            throw new InvalidOperationException("Fee structure is inactive.");
        }

        var details = await _structures.GetDetailsForStructureAsync(model.StructureId, cancellationToken);
        if (isApplicationChallan)
        {
            details = details.Where(IsAdmissionFeeLine).ToList();
        }

        if (details.Count == 0)
        {
            throw new InvalidOperationException(isApplicationChallan
                ? "No admission fee line found in the selected fee structure."
                : "Fee structure has no line items. Add structure details first.");
        }

        decimal totalAmount = 0;
        decimal lineDiscountTotal = 0;
        var linePayloads = new List<(short FeeHeadId, decimal Amount, decimal Discount, decimal LateFine, decimal Net)>();

        foreach (var line in details)
        {
            var concessionDiscount = isApplicationChallan
                ? 0m
                : await _concessions.GetApplicableDiscountForHeadAsync(
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
                    (ChallanNo, StudentID, ApplicationUid, StructureID, Semester, AcademicYear, IssueDate, DueDate,
                     TotalAmount, DiscountAmount, LateFineAmount, NetPayable, AmountPaid, Status, Remarks,
                     IsActive, CreatedBy, CreatedAt)
                VALUES
                    (@ChallanNo, @StudentId, @ApplicationUid, @StructureId, @Semester, @AcademicYear, @IssueDate, @DueDate,
                     @TotalAmount, @DiscountAmount, 0, @NetPayable, 0, 'Unpaid', @Remarks,
                     1, @CreatedBy, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;

            int challanId;
            await using (var command = new SqlCommand(insertChallan, connection, transaction))
            {
                command.Parameters.AddWithValue("@ChallanNo", challanNo);
                command.Parameters.AddWithValue("@StudentId", isApplicationChallan ? DBNull.Value : model.StudentId);
                command.Parameters.AddWithValue("@ApplicationUid", isApplicationChallan ? model.ApplicationUid!.Value : DBNull.Value);
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

    public async Task<bool> DeleteCancelledAsync(int challanId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string validateSql = """
                SELECT Status, AmountPaid
                FROM dbo.Challans
                WHERE Uid = @Uid;
                """;

            string status;
            decimal amountPaid;

            await using (var validateCommand = new SqlCommand(validateSql, connection, (SqlTransaction)transaction))
            {
                validateCommand.Parameters.AddWithValue("@Uid", challanId);
                await using var reader = await validateCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                status = reader["Status"] as string ?? string.Empty;
                amountPaid = FeeSql.ToDecimal(reader, "AmountPaid");
            }

            if (!string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (amountPaid > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            const string paymentCountSql = "SELECT COUNT(1) FROM dbo.Payments WHERE ChallanID = @Uid;";
            await using (var paymentCountCommand = new SqlCommand(paymentCountSql, connection, (SqlTransaction)transaction))
            {
                paymentCountCommand.Parameters.AddWithValue("@Uid", challanId);
                var paymentCount = Convert.ToInt32(await paymentCountCommand.ExecuteScalarAsync(cancellationToken));
                if (paymentCount > 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            const string deleteDetailsSql = "DELETE FROM dbo.ChallanDetails WHERE ChallanID = @Uid;";
            await using (var deleteDetailsCommand = new SqlCommand(deleteDetailsSql, connection, (SqlTransaction)transaction))
            {
                deleteDetailsCommand.Parameters.AddWithValue("@Uid", challanId);
                await deleteDetailsCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string deleteChallanSql = "DELETE FROM dbo.Challans WHERE Uid = @Uid AND Status = 'Cancelled';";
            await using (var deleteChallanCommand = new SqlCommand(deleteChallanSql, connection, (SqlTransaction)transaction))
            {
                deleteChallanCommand.Parameters.AddWithValue("@Uid", challanId);
                var deleted = await deleteChallanCommand.ExecuteNonQueryAsync(cancellationToken);
                if (deleted == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

    public async Task<IReadOnlyList<BulkChallanEligibleStudent>> GetEligibleStudentsAsync(
        int programId,
        string semester,
        short academicYear,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.StudentID,
                s.RegistrationNo,
                se.RollNo,
                s.StudentName,
                p.ProgramName,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Concessions con
                    WHERE con.StudentID = s.StudentID
                      AND con.IsActive = 1
                      AND con.ValidFrom <= CONVERT(date, SYSUTCDATETIME())
                      AND (con.ValidTo IS NULL OR con.ValidTo >= CONVERT(date, SYSUTCDATETIME()))
                ) THEN 1 ELSE 0 END AS HasConcession,
                CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Challans ch
                    WHERE ch.StudentID = s.StudentID
                      AND ch.Semester = @Semester
                      AND ch.AcademicYear = @AcademicYear
                      AND ch.IsActive = 1
                ) THEN 1 ELSE 0 END AS AlreadyHasChallan
            FROM dbo.Students s
            INNER JOIN dbo.StudentEnrollments se ON se.StudentID = s.StudentID AND se.IsActive = 1
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            WHERE se.ProgramID = @ProgramId
              AND s.IsActive = 1
            ORDER BY s.RegistrationNo;
            """;

        var list = new List<BulkChallanEligibleStudent>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@Semester", semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", academicYear);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new BulkChallanEligibleStudent
            {
                StudentId = FeeSql.ToInt32(reader, "StudentID"),
                RegistrationNo = reader["RegistrationNo"] as string ?? "",
                RollNo = reader["RollNo"] as string,
                StudentName = reader["StudentName"] as string ?? "",
                ProgramName = reader["ProgramName"] as string ?? "",
                HasConcession = FeeSql.ToInt32(reader, "HasConcession") == 1,
                AlreadyHasChallan = FeeSql.ToInt32(reader, "AlreadyHasChallan") == 1
            });
        }

        return list;
    }

    public async Task<BulkChallanGenerateResponse> BulkGenerateAsync(
        BulkChallanGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProgramId <= 0)
        {
            throw new InvalidOperationException("Program is required.");
        }

        if (request.StructureId <= 0)
        {
            throw new InvalidOperationException("Fee structure is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Semester))
        {
            throw new InvalidOperationException("Semester is required.");
        }

        if (request.AcademicYear is < 1900 or > 9999)
        {
            throw new InvalidOperationException("Academic year must be a valid 4-digit year.");
        }

        if (request.IssueDate > request.DueDate)
        {
            throw new InvalidOperationException("Issue date must be on or before due date.");
        }

        var studentIdsCsv = request.StudentIds is { Count: > 0 }
            ? string.Join(',', request.StudentIds.Where(id => id > 0).Distinct())
            : null;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("dbo.usp_BulkGenerateChallans", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ProgramID", request.ProgramId);
        command.Parameters.AddWithValue("@StructureID", request.StructureId);
        command.Parameters.AddWithValue("@Semester", request.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", request.AcademicYear);
        command.Parameters.AddWithValue("@IssueDate", request.IssueDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@DueDate", request.DueDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@CreatedBy", request.CreatedBy);
        command.Parameters.AddWithValue("@StudentIDs", (object?)studentIdsCsv ?? DBNull.Value);

        var results = new List<BulkChallanGenerateResultItem>();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = FeeSql.ToInt32(reader, "StudentID"),
                    RegistrationNo = reader["RegistrationNo"] as string ?? "",
                    StudentName = reader["StudentName"] as string ?? "",
                    ChallanNo = reader["ChallanNo"] is DBNull ? null : reader["ChallanNo"] as string,
                    NetPayable = reader["NetPayable"] is DBNull ? null : FeeSql.ToDecimal(reader, "NetPayable"),
                    Status = reader["Status"] as string ?? ""
                });
            }
        }
        catch (SqlException ex) when (ex.Number is 2812 or 208)
        {
            throw new InvalidOperationException(
                "Bulk challan stored procedure is not installed. Run Scripts/usp_BulkGenerateChallans.sql on the database.",
                ex);
        }

        var generated = results.Count(r => r.Status.Equals("Generated", StringComparison.OrdinalIgnoreCase));
        var skipped = results.Count(r => r.Status.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase));
        var errors = results.Count(r => r.Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase));

        return new BulkChallanGenerateResponse
        {
            TotalProcessed = results.Count,
            TotalGenerated = generated,
            TotalSkipped = skipped,
            TotalErrors = errors,
            Results = results
        };
    }

    private static bool IsAdmissionFeeLine(FeeStructureDetailLine line) =>
        string.Equals(line.FeeHeadCode, "ADM", StringComparison.OrdinalIgnoreCase)
        || line.FeeHeadName.Contains("Admission", StringComparison.OrdinalIgnoreCase);

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
