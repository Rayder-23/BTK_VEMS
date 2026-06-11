using Microsoft.Data.SqlClient;
using VEMS.Models;

namespace VEMS.Services;

public sealed class StudentApplicationRepository : IStudentApplicationRepository
{
    private const string StatusConfigKey = "ApplicationStatuses";
    private const string SourceConfigKey = "ApplicationSourceChannels";
    private const string TestStatusConfigKey = "ApplicationTestStatuses";
    private const string PaymentStatusConfigKey = "ApplicationPaymentStatuses";
    private const string GenderConfigKey = "Genders";
    private const string DefaultSourceChannel = "Online";
    private const string DefaultApplicationStatus = "Pending";
    private const string DefaultTestStatus = "NotScheduled";
    private const string DefaultPaymentStatus = "Pending";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configValues;

    public StudentApplicationRepository(IConfiguration configuration, IConfigurationValuesProvider configValues)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configValues = configValues;
    }

    public async Task<IReadOnlyList<StudentApplicationProgramOption>> GetActiveProgramsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                p.ProgramCode,
                p.ProgramName,
                p.DurationYears
            FROM dbo.ref_Programs p
            WHERE p.IsActive = 1
            ORDER BY p.ProgramName;
            """;

        var list = new List<StudentApplicationProgramOption>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentApplicationProgramOption
            {
                ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToByte(reader["DurationYears"])
            });
        }

        return list;
    }

    public Task<IReadOnlyList<string>> GetActiveCountryNamesAsync(CancellationToken cancellationToken = default) =>
        ReferenceListQueries.GetActiveCountryNamesAsync(_connectionString, cancellationToken);

    public Task<IReadOnlyList<string>> GetActiveProvinceNamesAsync(CancellationToken cancellationToken = default) =>
        ReferenceListQueries.GetActiveProvinceNamesAsync(_connectionString, cancellationToken);

    public Task<IReadOnlyList<string>> GetActiveCityNamesAsync(CancellationToken cancellationToken = default) =>
        ReferenceListQueries.GetActiveCityNamesAsync(_connectionString, cancellationToken);

    public async Task<string> InsertAsync(StudentApplicationFormModel model, CancellationToken cancellationToken = default)
    {
        var statuses = await _configValues.GetValuesAsync(StatusConfigKey, cancellationToken);
        var sources = await _configValues.GetValuesAsync(SourceConfigKey, cancellationToken);
        var testStatuses = await _configValues.GetValuesAsync(TestStatusConfigKey, cancellationToken);
        var paymentStatuses = await _configValues.GetValuesAsync(PaymentStatusConfigKey, cancellationToken);
        var genders = await _configValues.GetValuesAsync(GenderConfigKey, cancellationToken);

        EnsureConfig(StatusConfigKey, statuses);
        EnsureConfig(SourceConfigKey, sources);
        EnsureConfig(TestStatusConfigKey, testStatuses);
        EnsureConfig(PaymentStatusConfigKey, paymentStatuses);
        EnsureConfig(GenderConfigKey, genders);

        var applicationStatus = ResolveValue(statuses, DefaultApplicationStatus);
        var sourceChannel = ResolveValue(sources, DefaultSourceChannel);
        var testStatus = ResolveValue(testStatuses, DefaultTestStatus);
        var paymentStatus = ResolveValue(paymentStatuses, DefaultPaymentStatus);

        var gender = model.Gender.Trim().ToUpperInvariant();
        if (!genders.Any(g => string.Equals(g, gender, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Gender '{gender}' is not allowed. Configure '{GenderConfigKey}' in dbo.Configurations.");
        }

        var applicationNo = await GenerateApplicationNoAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        const string sql = """
            INSERT INTO dbo.StudentApplications (
                ApplicationNo,
                ApplicationDate,
                SourceChannel,
                InstTypeCode,
                ProgramCode,
                ProgramName,
                DesiredYear,
                DesiredGradeOrSemester,
                FirstName,
                LastName,
                FatherName,
                DateOfBirth,
                Gender,
                BFORM_No,
                NIC_No,
                MobileNo,
                EmailAddress,
                AddressLine1,
                City,
                Province,
                Country,
                FatherMobile,
                FatherOccupation,
                ApplicationStatus,
                TestStatus,
                PaymentStatus,
                CreatedAt
            )
            VALUES (
                @ApplicationNo,
                @ApplicationDate,
                @SourceChannel,
                @InstTypeCode,
                @ProgramCode,
                @ProgramName,
                @DesiredYear,
                @DesiredGradeOrSemester,
                @FirstName,
                @LastName,
                @FatherName,
                @DateOfBirth,
                @Gender,
                @BFORM_No,
                @NIC_No,
                @MobileNo,
                @EmailAddress,
                @AddressLine1,
                @City,
                @Province,
                @Country,
                @FatherMobile,
                @FatherOccupation,
                @ApplicationStatus,
                @TestStatus,
                @PaymentStatus,
                SYSUTCDATETIME()
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ApplicationNo", applicationNo);
        command.Parameters.AddWithValue("@ApplicationDate", today);
        command.Parameters.AddWithValue("@SourceChannel", sourceChannel);
        command.Parameters.AddWithValue("@InstTypeCode", StudentApplicationFieldDefaults.ResolveInstTypeCode(model.InstTypeCode));
        command.Parameters.AddWithValue("@ProgramCode", model.ProgramCode.Trim());
        command.Parameters.AddWithValue("@ProgramName", model.ProgramName.Trim());
        command.Parameters.AddWithValue("@DesiredYear", model.DesiredYear);
        command.Parameters.AddWithValue("@DesiredGradeOrSemester", model.DesiredGradeOrSemester);
        command.Parameters.AddWithValue("@FirstName", model.FirstName.Trim());
        command.Parameters.AddWithValue("@LastName", model.LastName.Trim());
        command.Parameters.AddWithValue("@FatherName", model.FatherName.Trim());
        command.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth!.Value.Date);
        command.Parameters.AddWithValue("@Gender", gender);
        command.Parameters.AddWithValue("@BFORM_No", (object?)model.BFORM_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@NIC_No", (object?)model.NIC_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@MobileNo", model.MobileNo.Trim());
        command.Parameters.AddWithValue("@EmailAddress", (object?)model.EmailAddress?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@AddressLine1", model.AddressLine1.Trim());
        command.Parameters.AddWithValue("@City", (object?)model.City?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Province", (object?)model.Province?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Country", ResolveCountryName(model.Country));
        command.Parameters.AddWithValue("@FatherMobile", (object?)model.FatherMobile?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@FatherOccupation", (object?)model.FatherOccupation?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ApplicationStatus", applicationStatus);
        command.Parameters.AddWithValue("@TestStatus", testStatus);
        command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return applicationNo;
    }

    private async Task<string> GenerateApplicationNoAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"APP-{year}-";

        const string sql = """
            SELECT MAX(ApplicationNo)
            FROM dbo.StudentApplications
            WHERE ApplicationNo LIKE @Prefix + '%';
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Prefix", prefix);
        await connection.OpenAsync(cancellationToken);
        var last = await command.ExecuteScalarAsync(cancellationToken) as string;

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last) && last.Length > prefix.Length)
        {
            var suffix = last[prefix.Length..];
            if (int.TryParse(suffix, out var parsed))
            {
                next = parsed + 1;
            }
        }

        return prefix + next.ToString("D5");
    }

    private static void EnsureConfig(string key, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException(
                $"Configuration '{key}' is missing or empty. Run Scripts/Seed_ApplicationConfigurations.sql.");
        }
    }

    private static string ResolveValue(IReadOnlyList<string> allowed, string preferred)
    {
        var match = allowed.FirstOrDefault(v => string.Equals(v, preferred, StringComparison.OrdinalIgnoreCase));
        return match ?? allowed[0];
    }

    private static string ResolveCountryName(string? country) =>
        string.IsNullOrWhiteSpace(country) ? "Pakistan" : country.Trim();
}
