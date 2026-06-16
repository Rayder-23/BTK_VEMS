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
                   c.Semester, c.AcademicYear, c.ChallanMonth, c.ChallanYear,
                   c.DueDate, c.NetPayable, c.AmountPaid, c.Status
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
                         AND se.ProgramID = @ProgramId)
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
                ChallanMonth = reader["ChallanMonth"] as string ?? "",
                ChallanYear = reader["ChallanYear"] as string ?? "",
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
                   c.Semester, c.AcademicYear, c.ChallanMonth, c.ChallanYear,
                   c.DueDate, c.NetPayable, c.AmountPaid, c.Status
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
                ChallanMonth = reader["ChallanMonth"] as string ?? "",
                ChallanYear = reader["ChallanYear"] as string ?? "",
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

        if (!isApplicationChallan
            && await ChallanExistsForBillingPeriodAsync(model.StudentId, model.IssueDate, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A challan already exists for {model.IssueDate:MMMM yyyy}. Cancel the existing challan or choose a different issue date.");
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

        details = FeeStructureDetailBilling.FilterForBillingPeriod(details, model.IssueDate);

        if (details.Count == 0)
        {
            var periodLabel = model.IssueDate.ToString("MMMM yyyy");
            throw new InvalidOperationException(isApplicationChallan
                ? $"No admission fee line applies for {periodLabel} (issue date month/year)."
                : $"No fee line items apply for {periodLabel}. Monthly heads are always included; one-time heads only match their configured month and year.");
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

        var (challanMonth, challanYear) = FeeStructureDetailBilling.ResolveBillingLabels(model.IssueDate);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        return await InsertChallanAsync(
            connection,
            transaction,
            isApplicationChallan ? 0 : model.StudentId,
            isApplicationChallan ? model.ApplicationUid : null,
            model.StructureId,
            structure.Semester,
            structure.AcademicYear,
            model.IssueDate,
            model.DueDate,
            totalAmount,
            discountAmount,
            netPayable,
            model.Remarks,
            challanMonth,
            challanYear,
            createdBy,
            linePayloads,
            cancellationToken);
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
        DateOnly issueDate,
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
                      AND MONTH(ch.IssueDate) = MONTH(@IssueDate)
                      AND YEAR(ch.IssueDate) = YEAR(@IssueDate)
                      AND ch.IsActive = 1
                      AND ch.Status <> 'Cancelled'
                ) THEN 1 ELSE 0 END AS AlreadyHasChallan
            FROM dbo.Students s
            INNER JOIN dbo.StudentEnrollments se ON se.StudentID = s.StudentID
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            WHERE se.ProgramID = @ProgramId
              AND s.IsActive = 1
            ORDER BY s.RegistrationNo;
            """;

        var list = new List<BulkChallanEligibleStudent>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@IssueDate", issueDate.ToDateTime(TimeOnly.MinValue));
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

        var studentIds = request.StudentIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (studentIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one student.");
        }

        var studentsById = await LoadBulkStudentsForProgramAsync(request.ProgramId, studentIds, cancellationToken);
        var results = new List<BulkChallanGenerateResultItem>();

        foreach (var studentId in studentIds)
        {
            if (!studentsById.TryGetValue(studentId, out var student))
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    Status = "Skipped - Not in program"
                });
                continue;
            }

            if (!student.IsActive)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = "Skipped - Inactive"
                });
                continue;
            }

            if (await ChallanExistsForBillingPeriodAsync(studentId, request.IssueDate, cancellationToken))
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Skipped - Challan exists for {request.IssueDate:MMMM yyyy}"
                });
                continue;
            }

            try
            {
                var challanId = await GenerateChallanAsync(
                    new ChallanGenerateFormModel
                    {
                        StudentId = studentId,
                        StructureId = request.StructureId,
                        IssueDate = request.IssueDate,
                        DueDate = request.DueDate
                    },
                    request.CreatedBy,
                    cancellationToken);

                var created = await ReadCreatedChallanSummaryAsync(challanId, cancellationToken);
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    ChallanNo = created?.ChallanNo,
                    NetPayable = created?.NetPayable,
                    Status = "Generated"
                });
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Error - {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Error - {ex.Message}"
                });
            }
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

    public async Task<IReadOnlyList<BulkChallanEligibleStudent>> GetEligibleStudentsForBillingRangeAsync(
        int programId,
        DateOnly fromPeriod,
        DateOnly toPeriod,
        CancellationToken cancellationToken = default)
    {
        var from = FeeStructureDetailBilling.NormalizeBillingMonth(fromPeriod);
        var to = FeeStructureDetailBilling.NormalizeBillingMonth(toPeriod);
        FeeStructureDetailBilling.ValidateBillingRange(from, to);

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
                      AND ch.IsActive = 1
                      AND ch.Status <> 'Cancelled'
                      AND DATEFROMPARTS(YEAR(ch.IssueDate), MONTH(ch.IssueDate), 1)
                          BETWEEN @FromPeriod AND @ToPeriod
                ) THEN 1 ELSE 0 END AS AlreadyHasChallan
            FROM dbo.Students s
            INNER JOIN dbo.StudentEnrollments se ON se.StudentID = s.StudentID
            INNER JOIN dbo.Programs p ON se.ProgramID = p.ProgramID
            WHERE se.ProgramID = @ProgramId
              AND s.IsActive = 1
            ORDER BY s.RegistrationNo;
            """;

        var list = new List<BulkChallanEligibleStudent>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@FromPeriod", from.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@ToPeriod", to.ToDateTime(TimeOnly.MinValue));
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

    public async Task<BulkChallanGenerateResponse> BulkGenerateMultiMonthAsync(
        BulkMultiMonthChallanGenerateRequest request,
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

        var fromPeriod = FeeStructureDetailBilling.NormalizeBillingMonth(request.FromPeriod);
        var toPeriod = FeeStructureDetailBilling.NormalizeBillingMonth(request.ToPeriod);
        FeeStructureDetailBilling.ValidateBillingRange(fromPeriod, toPeriod);

        if (request.IssueDate > request.DueDate)
        {
            throw new InvalidOperationException("Issue date must be on or before due date.");
        }

        var studentIds = request.StudentIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (studentIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one student.");
        }

        var periodLabel = FeeStructureDetailBilling.FormatBillingRange(fromPeriod, toPeriod);
        var studentsById = await LoadBulkStudentsForProgramAsync(request.ProgramId, studentIds, cancellationToken);
        var results = new List<BulkChallanGenerateResultItem>();

        foreach (var studentId in studentIds)
        {
            if (!studentsById.TryGetValue(studentId, out var student))
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    Status = "Skipped - Not in program"
                });
                continue;
            }

            if (!student.IsActive)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = "Skipped - Inactive"
                });
                continue;
            }

            if (await ChallanExistsForBillingRangeAsync(studentId, fromPeriod, toPeriod, cancellationToken))
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Skipped - Challan exists for {periodLabel}"
                });
                continue;
            }

            try
            {
                var challanId = await GenerateMultiMonthChallanAsync(
                    studentId,
                    request.StructureId,
                    fromPeriod,
                    toPeriod,
                    request.IssueDate,
                    request.DueDate,
                    request.CreatedBy,
                    cancellationToken);

                var created = await ReadCreatedChallanSummaryAsync(challanId, cancellationToken);
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    ChallanNo = created?.ChallanNo,
                    NetPayable = created?.NetPayable,
                    Status = "Generated"
                });
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Error - {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkChallanGenerateResultItem
                {
                    StudentId = studentId,
                    RegistrationNo = student.RegistrationNo,
                    StudentName = student.StudentName,
                    Status = $"Error - {ex.Message}"
                });
            }
        }

        return new BulkChallanGenerateResponse
        {
            TotalProcessed = results.Count,
            TotalGenerated = results.Count(r => r.Status.Equals("Generated", StringComparison.OrdinalIgnoreCase)),
            TotalSkipped = results.Count(r => r.Status.StartsWith("Skipped", StringComparison.OrdinalIgnoreCase)),
            TotalErrors = results.Count(r => r.Status.StartsWith("Error", StringComparison.OrdinalIgnoreCase)),
            Results = results
        };
    }

    private async Task<int> GenerateMultiMonthChallanAsync(
        int studentId,
        int structureId,
        DateOnly fromPeriod,
        DateOnly toPeriod,
        DateOnly issueDate,
        DateOnly dueDate,
        int createdBy,
        CancellationToken cancellationToken)
    {
        if (await ChallanExistsForBillingRangeAsync(studentId, fromPeriod, toPeriod, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A challan already exists for {FeeStructureDetailBilling.FormatBillingRange(fromPeriod, toPeriod)}.");
        }

        var structure = await _structures.GetAsync(structureId, cancellationToken)
            ?? throw new InvalidOperationException("Fee structure not found.");

        if (!structure.IsActive)
        {
            throw new InvalidOperationException("Fee structure is inactive.");
        }

        var details = await _structures.GetDetailsForStructureAsync(structureId, cancellationToken);
        var linePayloads = await BuildMultiMonthLinePayloadsAsync(
            studentId,
            details,
            fromPeriod,
            toPeriod,
            cancellationToken);

        if (linePayloads.Count == 0)
        {
            throw new InvalidOperationException(
                $"No fee line items apply for {FeeStructureDetailBilling.FormatBillingRange(fromPeriod, toPeriod)}. Monthly heads are included each month; one-time heads only match their configured month and year.");
        }

        decimal totalAmount = 0;
        decimal lineDiscountTotal = 0;
        foreach (var line in linePayloads)
        {
            totalAmount += line.Amount;
            lineDiscountTotal += line.Discount;
        }

        var netPayable = Math.Max(0, totalAmount - lineDiscountTotal);
        var remarks = $"Billing period: {FeeStructureDetailBilling.FormatBillingRange(fromPeriod, toPeriod)}";
        var (challanMonth, challanYear) = FeeStructureDetailBilling.ResolveBillingLabels(fromPeriod, toPeriod);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        return await InsertChallanAsync(
            connection,
            transaction,
            studentId,
            null,
            structureId,
            structure.Semester,
            structure.AcademicYear,
            issueDate,
            dueDate,
            totalAmount,
            lineDiscountTotal,
            netPayable,
            remarks,
            challanMonth,
            challanYear,
            createdBy,
            linePayloads,
            cancellationToken);
    }

    private async Task<List<(short FeeHeadId, decimal Amount, decimal Discount, decimal LateFine, decimal Net)>> BuildMultiMonthLinePayloadsAsync(
        int studentId,
        IReadOnlyList<FeeStructureDetailLine> details,
        DateOnly fromPeriod,
        DateOnly toPeriod,
        CancellationToken cancellationToken)
    {
        var totals = new Dictionary<short, (decimal Amount, decimal Discount)>();

        foreach (var period in FeeStructureDetailBilling.EnumerateBillingMonths(fromPeriod, toPeriod))
        {
            foreach (var line in FeeStructureDetailBilling.FilterForBillingPeriod(details, period))
            {
                var concessionDiscount = await _concessions.GetApplicableDiscountForHeadAsync(
                    studentId, line.FeeHeadId, line.Amount, period, cancellationToken);

                if (totals.TryGetValue(line.FeeHeadId, out var existing))
                {
                    totals[line.FeeHeadId] = (existing.Amount + line.Amount, existing.Discount + concessionDiscount);
                }
                else
                {
                    totals[line.FeeHeadId] = (line.Amount, concessionDiscount);
                }
            }
        }

        return totals
            .Select(entry =>
            {
                var amount = entry.Value.Amount;
                var discount = entry.Value.Discount;
                return (entry.Key, amount, discount, 0m, amount - discount);
            })
            .ToList();
    }

    private async Task<bool> ChallanExistsForBillingRangeAsync(
        int studentId,
        DateOnly fromPeriod,
        DateOnly toPeriod,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM dbo.Challans
            WHERE StudentID = @StudentId
              AND IsActive = 1
              AND Status <> 'Cancelled'
              AND DATEFROMPARTS(YEAR(IssueDate), MONTH(IssueDate), 1)
                  BETWEEN @FromPeriod AND @ToPeriod;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        command.Parameters.AddWithValue("@FromPeriod", FeeStructureDetailBilling.NormalizeBillingMonth(fromPeriod).ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@ToPeriod", FeeStructureDetailBilling.NormalizeBillingMonth(toPeriod).ToDateTime(TimeOnly.MinValue));
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    private static async Task<int> InsertChallanAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int studentId,
        int? applicationUid,
        int structureId,
        string semester,
        short academicYear,
        DateOnly issueDate,
        DateOnly dueDate,
        decimal totalAmount,
        decimal discountAmount,
        decimal netPayable,
        string? remarks,
        string challanMonth,
        string challanYear,
        int createdBy,
        IReadOnlyList<(short FeeHeadId, decimal Amount, decimal Discount, decimal LateFine, decimal Net)> linePayloads,
        CancellationToken cancellationToken)
    {
        try
        {
            var challanNo = await AllocateChallanNoAsync(connection, transaction, cancellationToken);
        const string insertChallan = """
            INSERT INTO dbo.Challans
                (ChallanNo, StudentID, ApplicationUid, StructureID, Semester, AcademicYear,
                 ChallanMonth, ChallanYear, IssueDate, DueDate,
                 TotalAmount, DiscountAmount, LateFineAmount, NetPayable, AmountPaid, Status, Remarks,
                 IsActive, CreatedBy, CreatedAt)
            VALUES
                (@ChallanNo, @StudentId, @ApplicationUid, @StructureId, @Semester, @AcademicYear,
                 @ChallanMonth, @ChallanYear, @IssueDate, @DueDate,
                 @TotalAmount, @DiscountAmount, 0, @NetPayable, 0, 'Unpaid', @Remarks,
                 1, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        int challanId;
        await using (var command = new SqlCommand(insertChallan, connection, transaction))
        {
            command.Parameters.AddWithValue("@ChallanNo", challanNo);
            command.Parameters.AddWithValue("@StudentId", applicationUid.HasValue ? DBNull.Value : studentId);
            command.Parameters.AddWithValue("@ApplicationUid", applicationUid.HasValue ? applicationUid.Value : DBNull.Value);
            command.Parameters.AddWithValue("@StructureId", structureId);
            command.Parameters.AddWithValue("@Semester", semester);
            command.Parameters.AddWithValue("@AcademicYear", academicYear);
            command.Parameters.AddWithValue("@ChallanMonth", challanMonth);
            command.Parameters.AddWithValue("@ChallanYear", challanYear);
            command.Parameters.AddWithValue("@IssueDate", issueDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@DueDate", dueDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@TotalAmount", totalAmount);
            command.Parameters.AddWithValue("@DiscountAmount", discountAmount);
            command.Parameters.AddWithValue("@NetPayable", netPayable);
            command.Parameters.AddWithValue("@Remarks", (object?)remarks?.Trim() ?? DBNull.Value);
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

    private sealed record BulkStudentRow(string RegistrationNo, string StudentName, bool IsActive);

    private async Task<Dictionary<int, BulkStudentRow>> LoadBulkStudentsForProgramAsync(
        int programId,
        IReadOnlyList<int> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return new Dictionary<int, BulkStudentRow>();
        }

        var idParameters = string.Join(", ", studentIds.Select((_, index) => $"@StudentId{index}"));
        var sql = $"""
            SELECT s.StudentID, s.RegistrationNo, s.StudentName, s.IsActive
            FROM dbo.Students s
            INNER JOIN dbo.StudentEnrollments se ON se.StudentID = s.StudentID
            WHERE se.ProgramID = @ProgramId
              AND s.StudentID IN ({idParameters});
            """;

        var map = new Dictionary<int, BulkStudentRow>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        for (var index = 0; index < studentIds.Count; index++)
        {
            command.Parameters.AddWithValue($"@StudentId{index}", studentIds[index]);
        }

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var studentId = FeeSql.ToInt32(reader, "StudentID");
            map[studentId] = new BulkStudentRow(
                reader["RegistrationNo"] as string ?? string.Empty,
                reader["StudentName"] as string ?? string.Empty,
                Convert.ToBoolean(reader["IsActive"]));
        }

        return map;
    }

    private async Task<bool> ChallanExistsForBillingPeriodAsync(
        int studentId,
        DateOnly issueDate,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM dbo.Challans
            WHERE StudentID = @StudentId
              AND MONTH(IssueDate) = MONTH(@IssueDate)
              AND YEAR(IssueDate) = YEAR(@IssueDate)
              AND IsActive = 1
              AND Status <> 'Cancelled';
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        command.Parameters.AddWithValue("@IssueDate", issueDate.ToDateTime(TimeOnly.MinValue));
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }

    private async Task<(string ChallanNo, decimal NetPayable)?> ReadCreatedChallanSummaryAsync(
        int challanId,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT ChallanNo, NetPayable FROM dbo.Challans WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", challanId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            reader["ChallanNo"] as string ?? string.Empty,
            FeeSql.ToDecimal(reader, "NetPayable"));
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
