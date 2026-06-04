using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Admissions;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services.Admissions;

public sealed class StudentApplicationAdminRepository : IStudentApplicationAdminRepository
{
    public const string ApprovedApplicationStatus = "Approved";
    public const string ConvertedApplicationStatus = "Converted As Student";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configValues;

    public StudentApplicationAdminRepository(IConfiguration configuration, IConfigurationValuesProvider configValues)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configValues = configValues;
    }

    public async Task<IReadOnlyList<StudentApplicationListItem>> ListAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ApplicationNo,
                ApplicationDate,
                FirstName,
                LastName,
                ProgramName,
                ApplicationStatus,
                SourceChannel,
                MobileNo
            FROM dbo.StudentApplications
            WHERE (@Search IS NULL OR ApplicationNo LIKE @Search OR FirstName LIKE @Search OR LastName LIKE @Search
                OR MobileNo LIKE @Search OR ProgramName LIKE @Search)
              AND (@Status IS NULL OR ApplicationStatus = @Status)
            ORDER BY ApplicationDate DESC, Uid DESC;
            """;

        var list = new List<StudentApplicationListItem>();
        var searchParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var statusParam = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", (object?)searchParam ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", (object?)statusParam ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentApplicationListItem
            {
                Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
                ApplicationNo = reader["ApplicationNo"] as string ?? string.Empty,
                ApplicationDate = reader.GetDateTime(reader.GetOrdinal("ApplicationDate")),
                FirstName = reader["FirstName"] as string ?? string.Empty,
                LastName = reader["LastName"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                ApplicationStatus = reader["ApplicationStatus"] as string ?? string.Empty,
                SourceChannel = reader["SourceChannel"] as string ?? string.Empty,
                MobileNo = reader["MobileNo"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<StudentApplicationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid, ApplicationNo, ApplicationDate, SourceChannel, InstTypeCode, ProgramCode, ProgramName,
                DesiredYear, DesiredGradeOrSemester, FirstName, LastName, FatherName, DateOfBirth, Gender,
                BFORM_No, NIC_No, MobileNo, EmailAddress, AddressLine1, City, Province,
                ISNULL(Country, N'Pakistan') AS Country, FatherMobile,
                FatherOccupation, ApplicationStatus, TestStatus, TestDate, TestScore, TeacherComments,
                PaymentStatus, PaymentAmount, PaymentDate, ConvertedStudentID, ConvertedAt
            FROM dbo.StudentApplications
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

        return MapForm(reader);
    }

    public async Task<StudentApplicationLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string programSql = """
            SELECT
                LTRIM(RTRIM(ISNULL(it.InstTypeCode, ''))) AS InstTypeCode,
                p.ProgramCode,
                p.ProgramName,
                p.TotalGrades,
                p.TotalSemesters
            FROM dbo.ref_Programs p
            LEFT JOIN dbo.ref_InstitutionTypes it ON it.Uid = p.InstTypeId
            WHERE p.IsActive = 1
            ORDER BY p.ProgramName;
            """;

        var programs = new List<VEMS.Models.StudentApplicationProgramOption>();
        await using (var connection = new SqlConnection(_connectionString))
        await using (var command = new SqlCommand(programSql, connection))
        {
            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                programs.Add(new VEMS.Models.StudentApplicationProgramOption
                {
                    InstTypeCode = reader["InstTypeCode"] as string ?? string.Empty,
                    ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
                    ProgramName = reader["ProgramName"] as string ?? string.Empty,
                    TotalGrades = reader["TotalGrades"] is DBNull ? null : Convert.ToByte(reader["TotalGrades"]),
                    TotalSemesters = reader["TotalSemesters"] is DBNull ? null : Convert.ToByte(reader["TotalSemesters"])
                });
            }
        }

        return new StudentApplicationLookups
        {
            Programs = programs,
            Countries = await ReferenceListQueries.GetActiveCountryNamesAsync(_connectionString, cancellationToken),
            Provinces = await ReferenceListQueries.GetActiveProvinceNamesAsync(_connectionString, cancellationToken),
            Cities = await ReferenceListQueries.GetActiveCityNamesAsync(_connectionString, cancellationToken),
            ApplicationStatuses = await _configValues.GetValuesAsync("ApplicationStatuses", cancellationToken),
            SourceChannels = await _configValues.GetValuesAsync("ApplicationSourceChannels", cancellationToken),
            TestStatuses = await _configValues.GetValuesAsync("ApplicationTestStatuses", cancellationToken),
            PaymentStatuses = await _configValues.GetValuesAsync("ApplicationPaymentStatuses", cancellationToken),
            Genders = await _configValues.GetValuesAsync("Genders", cancellationToken)
        };
    }

    public async Task<bool> ApplicationNoExistsAsync(string applicationNo, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM dbo.StudentApplications
            WHERE ApplicationNo = @ApplicationNo AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ApplicationNo", applicationNo.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<string> GenerateApplicationNoAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"APP-{year}-";

        const string sql = """
            SELECT MAX(ApplicationNo) FROM dbo.StudentApplications WHERE ApplicationNo LIKE @Prefix + '%';
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Prefix", prefix);
        await connection.OpenAsync(cancellationToken);
        var last = await command.ExecuteScalarAsync(cancellationToken) as string;

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last) && last.Length > prefix.Length && int.TryParse(last[prefix.Length..], out var parsed))
        {
            next = parsed + 1;
        }

        return prefix + next.ToString("D5");
    }

    public async Task<int> InsertAsync(StudentApplicationFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var applicationNo = string.IsNullOrWhiteSpace(model.ApplicationNo)
            ? await GenerateApplicationNoAsync(cancellationToken)
            : model.ApplicationNo.Trim();

        const string sql = """
            INSERT INTO dbo.StudentApplications (
                ApplicationNo, ApplicationDate, SourceChannel, InstTypeCode, ProgramCode, ProgramName,
                DesiredYear, DesiredGradeOrSemester, FirstName, LastName, FatherName, DateOfBirth, Gender,
                BFORM_No, NIC_No, MobileNo, EmailAddress, AddressLine1, City, Province, Country, FatherMobile,
                FatherOccupation, ApplicationStatus, TestStatus, TestDate, TestScore, TeacherComments,
                PaymentStatus, PaymentAmount, PaymentDate, ConvertedStudentID, ConvertedAt, CreatedAt
            )
            OUTPUT INSERTED.Uid
            VALUES (
                @ApplicationNo, @ApplicationDate, @SourceChannel, @InstTypeCode, @ProgramCode, @ProgramName,
                @DesiredYear, @DesiredGradeOrSemester, @FirstName, @LastName, @FatherName, @DateOfBirth, @Gender,
                @BFORM_No, @NIC_No, @MobileNo, @EmailAddress, @AddressLine1, @City, @Province, @Country, @FatherMobile,
                @FatherOccupation, @ApplicationStatus, @TestStatus, @TestDate, @TestScore, @TeacherComments,
                @PaymentStatus, @PaymentAmount, @PaymentDate, @ConvertedStudentID, @ConvertedAt, SYSUTCDATETIME()
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, model, applicationNo);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(StudentApplicationFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentApplications SET
                ApplicationNo = @ApplicationNo,
                ApplicationDate = @ApplicationDate,
                SourceChannel = @SourceChannel,
                InstTypeCode = @InstTypeCode,
                ProgramCode = @ProgramCode,
                ProgramName = @ProgramName,
                DesiredYear = @DesiredYear,
                DesiredGradeOrSemester = @DesiredGradeOrSemester,
                FirstName = @FirstName,
                LastName = @LastName,
                FatherName = @FatherName,
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                BFORM_No = @BFORM_No,
                NIC_No = @NIC_No,
                MobileNo = @MobileNo,
                EmailAddress = @EmailAddress,
                AddressLine1 = @AddressLine1,
                City = @City,
                Province = @Province,
                Country = @Country,
                FatherMobile = @FatherMobile,
                FatherOccupation = @FatherOccupation,
                ApplicationStatus = @ApplicationStatus,
                TestStatus = @TestStatus,
                TestDate = @TestDate,
                TestScore = @TestScore,
                TeacherComments = @TeacherComments,
                PaymentStatus = @PaymentStatus,
                PaymentAmount = @PaymentAmount,
                PaymentDate = @PaymentDate,
                ConvertedStudentID = @ConvertedStudentID,
                ConvertedAt = @ConvertedAt,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @UpdatedBy
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        AddParameters(command, model, model.ApplicationNo.Trim());
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.StudentApplications WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<AdmissionsDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        const string statsSql = """
            SELECT
                COUNT(*) AS TotalApplications,
                SUM(CASE WHEN ApplicationStatus = N'Pending' THEN 1 ELSE 0 END) AS PendingApplications,
                SUM(CASE WHEN ApplicationStatus = N'Approved' THEN 1 ELSE 0 END) AS ApprovedApplications,
                SUM(CASE WHEN PaymentStatus = N'Paid' THEN 1 ELSE 0 END) AS PaidApplications,
                SUM(CASE WHEN SourceChannel = N'Online' THEN 1 ELSE 0 END) AS OnlineApplications
            FROM dbo.StudentApplications;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        int total, pending, approved, paid, online;
        await using (var statsCmd = new SqlCommand(statsSql, connection))
        await using (var reader = await statsCmd.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            total = reader.GetInt32(reader.GetOrdinal("TotalApplications"));
            pending = reader.GetInt32(reader.GetOrdinal("PendingApplications"));
            approved = reader.GetInt32(reader.GetOrdinal("ApprovedApplications"));
            paid = reader.GetInt32(reader.GetOrdinal("PaidApplications"));
            online = reader.GetInt32(reader.GetOrdinal("OnlineApplications"));
        }

        var recent = await ListAsync(null, null, cancellationToken);
        return new AdmissionsDashboardViewModel
        {
            TotalApplications = total,
            PendingApplications = pending,
            ApprovedApplications = approved,
            PaidApplications = paid,
            OnlineApplications = online,
            RecentApplications = recent.Take(8).ToList()
        };
    }

    public async Task<IReadOnlyList<AdmissionsPaymentListItem>> ListPaymentsAsync(
        string? search,
        string? paymentStatus,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ApplicationNo,
                FirstName,
                LastName,
                PaymentStatus,
                PaymentAmount,
                PaymentDate,
                ApplicationStatus
            FROM dbo.StudentApplications
            WHERE (@Search IS NULL OR ApplicationNo LIKE @Search OR FirstName LIKE @Search OR LastName LIKE @Search)
              AND (@PaymentStatus IS NULL OR PaymentStatus = @PaymentStatus)
            ORDER BY PaymentDate DESC, Uid DESC;
            """;

        var list = new List<AdmissionsPaymentListItem>();
        var searchParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var statusParam = string.IsNullOrWhiteSpace(paymentStatus) ? null : paymentStatus.Trim();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", (object?)searchParam ?? DBNull.Value);
        command.Parameters.AddWithValue("@PaymentStatus", (object?)statusParam ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var first = reader["FirstName"] as string ?? string.Empty;
            var last = reader["LastName"] as string ?? string.Empty;
            list.Add(new AdmissionsPaymentListItem
            {
                Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
                ApplicationNo = reader["ApplicationNo"] as string ?? string.Empty,
                FullName = $"{first} {last}".Trim(),
                PaymentStatus = reader["PaymentStatus"] as string,
                PaymentAmount = reader["PaymentAmount"] is DBNull ? null : reader.GetDecimal(reader.GetOrdinal("PaymentAmount")),
                PaymentDate = reader["PaymentDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                ApplicationStatus = reader["ApplicationStatus"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<AdmissionsSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        const string totalSql = "SELECT COUNT(*) FROM dbo.StudentApplications;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var total = Convert.ToInt32(await new SqlCommand(totalSql, connection).ExecuteScalarAsync(cancellationToken));

        return new AdmissionsSummaryViewModel
        {
            TotalApplications = total,
            ByApplicationStatus = await ReadGroupCountsAsync(connection, "ApplicationStatus", cancellationToken),
            ByPaymentStatus = await ReadGroupCountsAsync(connection, "PaymentStatus", cancellationToken),
            BySourceChannel = await ReadGroupCountsAsync(connection, "SourceChannel", cancellationToken)
        };
    }

    public async Task<int> ConvertToStudentAsync(int applicationUid, int createdBy, CancellationToken cancellationToken = default)
    {
        var application = await GetAsync(applicationUid, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (!string.Equals(application.ApplicationStatus, ApprovedApplicationStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Only applications with status '{ApprovedApplicationStatus}' can be converted.");
        }

        if (application.ConvertedStudentID.HasValue)
        {
            throw new InvalidOperationException("This application has already been converted to a student.");
        }

        var statuses = await _configValues.GetValuesAsync("ApplicationStatuses", cancellationToken);
        if (!statuses.Any(s => string.Equals(s, ConvertedApplicationStatus, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Configuration 'ApplicationStatuses' must include '{ConvertedApplicationStatus}'. Run Scripts/Seed_ApplicationConfigurations.sql.");
        }

        var genders = await _configValues.GetValuesAsync("Genders", cancellationToken);
        StudentConversionValidator.ValidateApplicationFields(application, genders);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureEmployeeLoginExistsAsync(connection, createdBy, cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var programId = await ResolveProgramIdAsync(connection, transaction, application.ProgramCode, cancellationToken);
            var registrationNo = await ResolveRegistrationNoAsync(connection, transaction, application.ApplicationNo, cancellationToken);
            var countryId = await ResolveCountryIdAsync(connection, transaction, application.Country, cancellationToken);
            var provinceId = await ResolveProvinceIdAsync(connection, transaction, application.Province, cancellationToken);
            var cityId = await ResolveCityIdAsync(connection, transaction, application.City, provinceId, cancellationToken);
            await EnsureStudentReferenceIdsAsync(connection, transaction, programId, countryId, provinceId, cityId, cancellationToken);

            const string insertStudentSql = """
                INSERT INTO dbo.Students (
                    RegistrationNo, ProgramID, AdmissionYear, AdmissionDate,
                    FirstName, LastName, FatherName, DateOfBirth, Gender,
                    AddressLine1, CountryID, ProvinceID, CityID,
                    NIC_No, BFORM_No, Nationality, IsActive, StatusRemark,
                    PreviousGradeOrSem, CreatedBy
                )
                OUTPUT INSERTED.Uid
                VALUES (
                    @RegistrationNo, @ProgramID, @AdmissionYear, @AdmissionDate,
                    @FirstName, @LastName, @FatherName, @DateOfBirth, @Gender,
                    @AddressLine1, @CountryID, @ProvinceID, @CityID,
                    @NIC_No, @BFORM_No, @Nationality, @IsActive, @StatusRemark,
                    @PreviousGradeOrSem, @CreatedBy
                );
                """;

            await using var insertCmd = new SqlCommand(insertStudentSql, connection, transaction);
            insertCmd.Parameters.AddWithValue("@RegistrationNo", registrationNo);
            insertCmd.Parameters.AddWithValue("@ProgramID", programId);
            insertCmd.Parameters.AddWithValue("@AdmissionYear", application.DesiredYear);
            insertCmd.Parameters.AddWithValue("@AdmissionDate", application.ApplicationDate!.Value.Date);
            insertCmd.Parameters.AddWithValue("@FirstName", application.FirstName.Trim());
            insertCmd.Parameters.AddWithValue("@LastName", application.LastName.Trim());
            insertCmd.Parameters.AddWithValue("@FatherName", application.FatherName.Trim());
            insertCmd.Parameters.AddWithValue("@DateOfBirth", application.DateOfBirth!.Value.Date);
            insertCmd.Parameters.AddWithValue("@Gender", StudentConversionValidator.NormalizeGenderCode(application.Gender));
            insertCmd.Parameters.AddWithValue("@AddressLine1", application.AddressLine1.Trim());
            insertCmd.Parameters.AddWithValue("@CountryID", countryId);
            insertCmd.Parameters.AddWithValue("@ProvinceID", provinceId);
            insertCmd.Parameters.AddWithValue("@CityID", cityId);
            insertCmd.Parameters.AddWithValue("@NIC_No", (object?)application.NIC_No?.Trim() ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@BFORM_No", (object?)application.BFORM_No?.Trim() ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Nationality", "Pakistani");
            insertCmd.Parameters.AddWithValue("@IsActive", true);
            insertCmd.Parameters.AddWithValue("@StatusRemark", $"Converted from application {application.ApplicationNo}");
            insertCmd.Parameters.AddWithValue("@PreviousGradeOrSem", application.DesiredGradeOrSemester.ToString());
            insertCmd.Parameters.AddWithValue("@CreatedBy", createdBy);

            var studentId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync(cancellationToken));

            const string updateAppSql = """
                UPDATE dbo.StudentApplications
                SET ApplicationStatus = @ApplicationStatus,
                    ConvertedStudentID = @ConvertedStudentID,
                    ConvertedAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedBy = @UpdatedBy
                WHERE Uid = @Uid;
                """;

            await using var updateCmd = new SqlCommand(updateAppSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("@ApplicationStatus", ConvertedApplicationStatus);
            updateCmd.Parameters.AddWithValue("@ConvertedStudentID", studentId);
            updateCmd.Parameters.AddWithValue("@UpdatedBy", createdBy);
            updateCmd.Parameters.AddWithValue("@Uid", applicationUid);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return studentId;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(SqlForeignKeyViolationFormatter.Describe(ex), ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<int> ResolveProgramIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string programCode,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT Uid FROM dbo.ref_Programs WHERE ProgramCode = @ProgramCode AND IsActive = 1;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ProgramCode", programCode.Trim());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException(
                $"Program code '{programCode.Trim()}' was not found among active programs (ref_Programs). Edit the applicant and choose a valid program, or activate the program in the system.");
        }

        return Convert.ToInt32(result);
    }

    private static async Task<string> ResolveRegistrationNoAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string applicationNo,
        CancellationToken cancellationToken)
    {
        var candidate = applicationNo.Trim();
        if (!await RegistrationNoExistsAsync(connection, transaction, candidate, cancellationToken))
        {
            return candidate;
        }

        var suffix = 1;
        while (suffix < 100)
        {
            var alt = $"{candidate}-S{suffix}";
            if (!await RegistrationNoExistsAsync(connection, transaction, alt, cancellationToken))
            {
                return alt;
            }

            suffix++;
        }

        throw new InvalidOperationException("Could not generate a unique registration number.");
    }

    private static async Task<bool> RegistrationNoExistsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string registrationNo,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Students WHERE RegistrationNo = @RegistrationNo;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@RegistrationNo", registrationNo);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static async Task<int> ResolveCountryIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string? countryName,
        CancellationToken cancellationToken)
    {
        var name = string.IsNullOrWhiteSpace(countryName) ? "Pakistan" : countryName.Trim();

        const string matchSql = """
            SELECT TOP 1 Uid FROM dbo.ref_Countries
            WHERE IsActive = 1 AND CountryName LIKE @Name
            ORDER BY CASE WHEN CountryName = @Exact THEN 0 ELSE 1 END, Uid;
            """;
        await using var matchCmd = new SqlCommand(matchSql, connection, transaction);
        matchCmd.Parameters.AddWithValue("@Name", $"%{name}%");
        matchCmd.Parameters.AddWithValue("@Exact", name);
        var matched = await matchCmd.ExecuteScalarAsync(cancellationToken);
        if (matched is not null and not DBNull)
        {
            return Convert.ToInt32(matched);
        }

        throw new InvalidOperationException(
            $"Country '{name}' was not found in ref_Countries. Select a valid country on the application (default: Pakistan) or add it in reference data.");
    }

    private static async Task<int> ResolveProvinceIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string? provinceName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(provinceName))
        {
            const string matchSql = """
                SELECT TOP 1 Uid FROM dbo.ref_Provinces
                WHERE IsActive = 1 AND ProvinceName LIKE @Name
                ORDER BY Uid;
                """;
            await using var matchCmd = new SqlCommand(matchSql, connection, transaction);
            matchCmd.Parameters.AddWithValue("@Name", $"%{provinceName.Trim()}%");
            var matched = await matchCmd.ExecuteScalarAsync(cancellationToken);
            if (matched is not null and not DBNull)
            {
                return Convert.ToInt32(matched);
            }

            var samples = await SampleReferenceNamesAsync(connection, transaction, "ref_Provinces", "ProvinceName", cancellationToken);
            throw new InvalidOperationException(
                $"Province '{provinceName.Trim()}' does not match any active province in reference data. "
                + $"Correct the Province field on this application (active examples: {samples}).");
        }

        const string fallbackSql = "SELECT TOP 1 Uid FROM dbo.ref_Provinces WHERE IsActive = 1 ORDER BY Uid;";
        await using var fallbackCmd = new SqlCommand(fallbackSql, connection, transaction);
        var result = await fallbackCmd.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException(
                "No active province exists in ref_Provinces. Add provinces in reference data or enter a valid Province on the application.");
        }

        return Convert.ToInt32(result);
    }

    private static async Task<int> ResolveCityIdAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string? cityName,
        int provinceId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cityName))
        {
            const string matchSql = """
                SELECT TOP 1 Uid FROM dbo.ref_Cities
                WHERE IsActive = 1 AND CityName LIKE @Name
                ORDER BY Uid;
                """;
            await using var matchCmd = new SqlCommand(matchSql, connection, transaction);
            matchCmd.Parameters.AddWithValue("@Name", $"%{cityName.Trim()}%");
            var matched = await matchCmd.ExecuteScalarAsync(cancellationToken);
            if (matched is not null and not DBNull)
            {
                return Convert.ToInt32(matched);
            }

            var samples = await SampleReferenceNamesAsync(connection, transaction, "ref_Cities", "CityName", cancellationToken);
            throw new InvalidOperationException(
                $"City '{cityName.Trim()}' does not match any active city in reference data. "
                + $"Correct the City field on this application (active examples: {samples}).");
        }

        const string fallbackSql = """
            SELECT TOP 1 Uid FROM dbo.ref_Cities
            WHERE IsActive = 1 AND ProvinceID = @ProvinceID
            ORDER BY Uid;
            """;
        await using var fallbackCmd = new SqlCommand(fallbackSql, connection, transaction);
        fallbackCmd.Parameters.AddWithValue("@ProvinceID", provinceId);
        var result = await fallbackCmd.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            const string anySql = "SELECT TOP 1 Uid FROM dbo.ref_Cities WHERE IsActive = 1 ORDER BY Uid;";
            await using var anyCmd = new SqlCommand(anySql, connection, transaction);
            result = await anyCmd.ExecuteScalarAsync(cancellationToken);
        }

        if (result is null or DBNull)
        {
            throw new InvalidOperationException(
                "No active city exists in ref_Cities. Add cities in reference data or enter a valid City on the application.");
        }

        return Convert.ToInt32(result);
    }

    private static async Task EnsureEmployeeLoginExistsAsync(
        SqlConnection connection,
        int employeeLoginUid,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.EmployeeLogin WHERE Uid = @Uid;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", employeeLoginUid);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
        {
            throw new InvalidOperationException(
                $"Staff login id {employeeLoginUid} was not found in EmployeeLogin. Sign in again as an employee before converting applicants.");
        }
    }

    private static async Task EnsureStudentReferenceIdsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int programId,
        int countryId,
        int provinceId,
        int cityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ref_Programs WHERE Uid = @ProgramID AND IsActive = 1) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ref_Countries WHERE Uid = @CountryID AND IsActive = 1) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ref_Provinces WHERE Uid = @ProvinceID AND IsActive = 1) THEN 1 ELSE 0 END,
                CASE WHEN EXISTS (SELECT 1 FROM dbo.ref_Cities WHERE Uid = @CityID AND IsActive = 1) THEN 1 ELSE 0 END;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ProgramID", programId);
        command.Parameters.AddWithValue("@CountryID", countryId);
        command.Parameters.AddWithValue("@ProvinceID", provinceId);
        command.Parameters.AddWithValue("@CityID", cityId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return;
        }

        var issues = new List<string>();
        if (reader.GetInt32(0) == 0)
        {
            issues.Add("Program reference is missing or inactive (ref_Programs).");
        }

        if (reader.GetInt32(1) == 0)
        {
            issues.Add("Country reference is missing or inactive (ref_Countries).");
        }

        if (reader.GetInt32(2) == 0)
        {
            issues.Add("Province reference is missing or inactive (ref_Provinces).");
        }

        if (reader.GetInt32(3) == 0)
        {
            issues.Add("City reference is missing or inactive (ref_Cities).");
        }

        if (issues.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", issues));
        }
    }

    private static async Task<string> SampleReferenceNamesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ref_Provinces", "ref_Cities"
        };
        var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProvinceName", "CityName"
        };

        if (!allowedTables.Contains(tableName) || !allowedColumns.Contains(columnName))
        {
            return "see reference data";
        }

        var sql = $"""
            SELECT TOP 5 {columnName}
            FROM dbo.{tableName}
            WHERE IsActive = 1
            ORDER BY {columnName};
            """;

        var names = new List<string>();
        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader[0] as string;
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name.Trim());
            }
        }

        return names.Count > 0 ? string.Join(", ", names) : "none configured";
    }

    private static async Task<IReadOnlyList<AdmissionsStatusCount>> ReadGroupCountsAsync(
        SqlConnection connection,
        string column,
        CancellationToken cancellationToken)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ApplicationStatus", "PaymentStatus", "SourceChannel"
        };
        if (!allowed.Contains(column))
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        var sql = $"""
            SELECT {column} AS Label, COUNT(*) AS Cnt
            FROM dbo.StudentApplications
            GROUP BY {column}
            ORDER BY COUNT(*) DESC;
            """;

        var list = new List<AdmissionsStatusCount>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AdmissionsStatusCount
            {
                Label = reader["Label"] is DBNull ? "(blank)" : reader["Label"] as string ?? "(blank)",
                Count = reader.GetInt32(reader.GetOrdinal("Cnt"))
            });
        }

        return list;
    }

    private static void AddParameters(SqlCommand command, StudentApplicationFormModel model, string applicationNo)
    {
        command.Parameters.AddWithValue("@ApplicationNo", applicationNo);
        command.Parameters.AddWithValue("@ApplicationDate", model.ApplicationDate!.Value.Date);
        command.Parameters.AddWithValue("@SourceChannel", model.SourceChannel.Trim());
        command.Parameters.AddWithValue("@InstTypeCode", model.InstTypeCode.Trim());
        command.Parameters.AddWithValue("@ProgramCode", model.ProgramCode.Trim());
        command.Parameters.AddWithValue("@ProgramName", model.ProgramName.Trim());
        command.Parameters.AddWithValue("@DesiredYear", model.DesiredYear);
        command.Parameters.AddWithValue("@DesiredGradeOrSemester", model.DesiredGradeOrSemester);
        command.Parameters.AddWithValue("@FirstName", model.FirstName.Trim());
        command.Parameters.AddWithValue("@LastName", model.LastName.Trim());
        command.Parameters.AddWithValue("@FatherName", model.FatherName.Trim());
        command.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth!.Value.Date);
        command.Parameters.AddWithValue("@Gender", model.Gender.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@BFORM_No", (object?)model.BFORM_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@NIC_No", (object?)model.NIC_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@MobileNo", model.MobileNo.Trim());
        command.Parameters.AddWithValue("@EmailAddress", (object?)model.EmailAddress?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@AddressLine1", model.AddressLine1.Trim());
        command.Parameters.AddWithValue("@City", (object?)model.City?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Province", (object?)model.Province?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Country", string.IsNullOrWhiteSpace(model.Country) ? "Pakistan" : model.Country.Trim());
        command.Parameters.AddWithValue("@FatherMobile", (object?)model.FatherMobile?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@FatherOccupation", (object?)model.FatherOccupation?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ApplicationStatus", model.ApplicationStatus.Trim());
        command.Parameters.AddWithValue("@TestStatus", (object?)model.TestStatus?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@TestDate", model.TestDate.HasValue ? model.TestDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@TestScore", model.TestScore.HasValue ? model.TestScore.Value : DBNull.Value);
        command.Parameters.AddWithValue("@TeacherComments", (object?)model.TeacherComments?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@PaymentStatus", (object?)model.PaymentStatus?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@PaymentAmount", model.PaymentAmount.HasValue ? model.PaymentAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@PaymentDate", model.PaymentDate.HasValue ? model.PaymentDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@ConvertedStudentID", model.ConvertedStudentID.HasValue ? model.ConvertedStudentID.Value : DBNull.Value);
        command.Parameters.AddWithValue("@ConvertedAt", model.ConvertedAt.HasValue ? model.ConvertedAt.Value : DBNull.Value);
    }

    private static StudentApplicationFormModel MapForm(SqlDataReader reader) =>
        new()
        {
            Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
            ApplicationNo = reader["ApplicationNo"] as string ?? string.Empty,
            ApplicationDate = reader.GetDateTime(reader.GetOrdinal("ApplicationDate")),
            SourceChannel = reader["SourceChannel"] as string ?? string.Empty,
            InstTypeCode = reader["InstTypeCode"] as string ?? string.Empty,
            ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
            ProgramName = reader["ProgramName"] as string ?? string.Empty,
            DesiredYear = reader.GetInt16(reader.GetOrdinal("DesiredYear")),
            DesiredGradeOrSemester = reader.GetByte(reader.GetOrdinal("DesiredGradeOrSemester")),
            FirstName = reader["FirstName"] as string ?? string.Empty,
            LastName = reader["LastName"] as string ?? string.Empty,
            FatherName = reader["FatherName"] as string ?? string.Empty,
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Gender = (reader["Gender"] as string ?? "M").Trim(),
            BFORM_No = reader["BFORM_No"] as string,
            NIC_No = reader["NIC_No"] as string,
            MobileNo = reader["MobileNo"] as string ?? string.Empty,
            EmailAddress = reader["EmailAddress"] as string,
            AddressLine1 = reader["AddressLine1"] as string ?? string.Empty,
            City = reader["City"] as string,
            Province = reader["Province"] as string,
            Country = reader["Country"] as string ?? "Pakistan",
            FatherMobile = reader["FatherMobile"] as string,
            FatherOccupation = reader["FatherOccupation"] as string,
            ApplicationStatus = reader["ApplicationStatus"] as string ?? string.Empty,
            TestStatus = reader["TestStatus"] as string,
            TestDate = reader["TestDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("TestDate")),
            TestScore = reader["TestScore"] is DBNull ? null : reader.GetDecimal(reader.GetOrdinal("TestScore")),
            TeacherComments = reader["TeacherComments"] as string,
            PaymentStatus = reader["PaymentStatus"] as string,
            PaymentAmount = reader["PaymentAmount"] is DBNull ? null : reader.GetDecimal(reader.GetOrdinal("PaymentAmount")),
            PaymentDate = reader["PaymentDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
            ConvertedStudentID = reader["ConvertedStudentID"] is DBNull ? null : reader.GetInt32(reader.GetOrdinal("ConvertedStudentID")),
            ConvertedAt = reader["ConvertedAt"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("ConvertedAt"))
        };
}
