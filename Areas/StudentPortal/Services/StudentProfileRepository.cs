using Microsoft.Data.SqlClient;
using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentProfileRepository : IStudentProfileRepository
{
    private readonly string _connectionString;

    public StudentProfileRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<StudentProfileViewModel?> GetByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.Uid,
                s.RegistrationNo,
                s.RollNo,
                s.FirstName,
                s.MiddleName,
                s.LastName,
                s.FatherName,
                s.DateOfBirth,
                s.Gender,
                s.Nationality,
                s.NIC_No,
                s.BFORM_No,
                s.AdmissionYear,
                s.AdmissionDate,
                s.AddressLine1,
                s.AddressLine2,
                s.PostalCode,
                s.IsActive,
                s.StatusRemark,
                p.ProgramName,
                c.CountryName,
                pr.ProvinceName,
                ci.CityName,
                sl.Username,
                sl.Email,
                sl.Status AS LoginStatus,
                sl.LastLoginAt
            FROM dbo.Students s
            LEFT JOIN dbo.ref_Programs p ON s.ProgramID = p.Uid
            LEFT JOIN dbo.ref_Countries c ON s.CountryID = c.Uid
            LEFT JOIN dbo.ref_Provinces pr ON s.ProvinceID = pr.Uid
            LEFT JOIN dbo.ref_Cities ci ON s.CityID = ci.Uid
            LEFT JOIN dbo.StudentsLogin sl ON sl.StudentId = s.Uid
            WHERE s.Uid = @StudentUid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var first = reader["FirstName"] as string ?? string.Empty;
        var middle = reader["MiddleName"] as string;
        var last = reader["LastName"] as string ?? string.Empty;
        var fullName = string.IsNullOrWhiteSpace(middle)
            ? $"{first} {last}".Trim()
            : $"{first} {middle} {last}".Trim();

        return new StudentProfileViewModel
        {
            Uid = ToInt32(reader, "Uid"),
            RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
            RollNo = reader["RollNo"] as string,
            FullName = fullName,
            FatherName = reader["FatherName"] as string ?? string.Empty,
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Gender = (reader["Gender"] as string ?? "M").Trim(),
            Nationality = reader["Nationality"] as string,
            NicNo = reader["NIC_No"] as string,
            BformNo = reader["BFORM_No"] as string,
            AdmissionYear = reader.GetInt16(reader.GetOrdinal("AdmissionYear")),
            AdmissionDate = reader.GetDateTime(reader.GetOrdinal("AdmissionDate")),
            AddressLine1 = reader["AddressLine1"] as string ?? string.Empty,
            AddressLine2 = reader["AddressLine2"] as string,
            PostalCode = reader["PostalCode"] as string,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            StatusRemark = reader["StatusRemark"] as string,
            ProgramName = reader["ProgramName"] as string,
            CountryName = reader["CountryName"] as string,
            ProvinceName = reader["ProvinceName"] as string,
            CityName = reader["CityName"] as string,
            PortalUsername = reader["Username"] as string,
            PortalEmail = reader["Email"] as string,
            PortalStatus = reader["LoginStatus"] as string,
            LastLoginAt = reader["LastLoginAt"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt"))
        };
    }

    public async Task<int?> ResolveStudentUidByLoginUidAsync(int loginUid, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT StudentId FROM dbo.StudentsLogin WHERE Uid = @LoginUid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@LoginUid", loginUid);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    public async Task<string?> GetPasswordHashByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT PasswordHash FROM dbo.StudentsLogin WHERE StudentId = @StudentUid;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : result.ToString();
    }

    public async Task<bool> UpdatePasswordAsync(int studentUid, string passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.StudentsLogin
            SET PasswordHash = @PasswordHash,
                PasswordChangedAt = SYSUTCDATETIME(),
                MustChangePassword = 0
            WHERE StudentId = @StudentUid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentUid", studentUid);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static int ToInt32(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));
}
