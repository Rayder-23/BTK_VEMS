using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Services;
using VEMS.Services;

namespace VEMS.Areas.TeacherPortal.Services;

public sealed class TeacherLoginRepository : ITeacherLoginRepository
{
    private const string EmployeeRolesKey = "EmployeeRoles";
    private const string EmployeeStatusKey = "EmployeeStatus";
    private const string TeacherRoleName = "Teacher";
    private const string ActiveStatusName = "Active";

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public TeacherLoginRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<TeacherLoginValidationResult> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var trimmedUsername = username.Trim();

        const string sql = """
            SELECT TOP (1)
                el.Uid,
                el.EmployeeId,
                el.Username,
                el.PasswordHash,
                el.Role,
                el.Status,
                e.EmployeeId AS EmployeeCode,
                e.FullName
            FROM dbo.EmployeeLogin el
            INNER JOIN dbo.Employee e ON e.Uid = el.EmployeeId
            WHERE el.Username = @username;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@username", trimmedUsername);

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return TeacherLoginValidationResult.Failure(TeacherLoginFailureReason.InvalidCredentials);
        }

        var storedHash = reader["PasswordHash"] as string ?? string.Empty;
        if (!StudentPasswordHasher.VerifyPassword(password, storedHash))
        {
            return TeacherLoginValidationResult.Failure(TeacherLoginFailureReason.InvalidCredentials);
        }

        var role = reader["Role"] as string ?? string.Empty;
        var status = reader["Status"] as string ?? string.Empty;

        var employeeRoles = await _configurations.GetValuesAsync(EmployeeRolesKey, cancellationToken);
        var employeeStatuses = await _configurations.GetValuesAsync(EmployeeStatusKey, cancellationToken);

        var teacherRole = employeeRoles.FirstOrDefault(
            value => string.Equals(value, TeacherRoleName, StringComparison.OrdinalIgnoreCase))
            ?? TeacherRoleName;

        if (!string.Equals(role, teacherRole, StringComparison.OrdinalIgnoreCase))
        {
            return TeacherLoginValidationResult.Failure(TeacherLoginFailureReason.NotTeacherRole);
        }

        var activeStatus = employeeStatuses.FirstOrDefault(
            value => string.Equals(value, ActiveStatusName, StringComparison.OrdinalIgnoreCase))
            ?? ActiveStatusName;

        if (!string.Equals(status, activeStatus, StringComparison.OrdinalIgnoreCase))
        {
            return TeacherLoginValidationResult.Failure(TeacherLoginFailureReason.InactiveAccount);
        }

        return TeacherLoginValidationResult.Success(new TeacherLoginUser
        {
            LoginUid = Convert.ToInt32(reader["Uid"]),
            EmployeeUid = Convert.ToInt32(reader["EmployeeId"]),
            Username = reader["Username"] as string ?? trimmedUsername,
            DisplayName = reader["FullName"] as string ?? trimmedUsername,
            EmployeeCode = reader["EmployeeCode"] as string ?? string.Empty,
            Role = role
        });
    }
}
