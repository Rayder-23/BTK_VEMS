using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly string _connectionString;

    public EmployeeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<EmployeeListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                EmployeeId,
                FullName,
                Department,
                Designation,
                Status
            FROM dbo.Employee
            ORDER BY Uid DESC;
            """;

        var list = new List<EmployeeListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new EmployeeListItemViewModel
            {
                Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
                EmployeeId = reader["EmployeeId"] as string ?? string.Empty,
                FullName = reader["FullName"] as string ?? string.Empty,
                Department = reader["Department"] as string,
                Designation = reader["Designation"] as string,
                Status = reader["Status"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<EmployeeFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                EmployeeId,
                FullName,
                Email,
                Phone,
                CNIC,
                FatherName,
                DOB,
                Department,
                Designation,
                Specialization,
                Qualification,
                EmployeeType,
                Status,
                JoinedDate,
                Notes
            FROM dbo.Employee
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

        return MapRow(reader);
    }

    public async Task<int> InsertAsync(EmployeeFormModel model, CancellationToken cancellationToken = default)
    {
        if (!model.JoinedDate.HasValue)
        {
            throw new ArgumentException("JoinedDate is required.", nameof(model));
        }

        var now = DateTime.UtcNow;
        const string sql = """
            INSERT INTO dbo.Employee (
                EmployeeId,
                FullName,
                Email,
                Phone,
                CNIC,
                FatherName,
                DOB,
                Department,
                Designation,
                Specialization,
                Qualification,
                EmployeeType,
                Status,
                JoinedDate,
                Notes,
                CreatedAt,
                ModifiedAt
            )
            OUTPUT INSERTED.Uid
            VALUES (
                @EmployeeId,
                @FullName,
                @Email,
                @Phone,
                @CNIC,
                @FatherName,
                @DOB,
                @Department,
                @Designation,
                @Specialization,
                @Qualification,
                @EmployeeType,
                @Status,
                @JoinedDate,
                @Notes,
                @CreatedAt,
                @ModifiedAt
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddWriteParameters(command, model);
        command.Parameters.AddWithValue("@CreatedAt", now);
        command.Parameters.AddWithValue("@ModifiedAt", now);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(EmployeeFormModel model, CancellationToken cancellationToken = default)
    {
        if (!model.JoinedDate.HasValue)
        {
            throw new ArgumentException("JoinedDate is required.", nameof(model));
        }

        const string sql = """
            UPDATE dbo.Employee
            SET
                EmployeeId = @EmployeeId,
                FullName = @FullName,
                Email = @Email,
                Phone = @Phone,
                CNIC = @CNIC,
                FatherName = @FatherName,
                DOB = @DOB,
                Department = @Department,
                Designation = @Designation,
                Specialization = @Specialization,
                Qualification = @Qualification,
                EmployeeType = @EmployeeType,
                Status = @Status,
                JoinedDate = @JoinedDate,
                Notes = @Notes,
                ModifiedAt = @ModifiedAt
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        AddWriteParameters(command, model);
        command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Employee WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId, int? excludeUid = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Employee
            WHERE EmployeeId = @EmployeeId
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<EmployeeTeacherLookupResult?> GetByEmployeeIdForTeacherAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return null;
        }

        const string sql = """
            SELECT
                EmployeeId,
                FullName,
                Email,
                Phone,
                Designation,
                Qualification,
                Specialization,
                JoinedDate,
                Status
            FROM dbo.Employee
            WHERE EmployeeId = @EmployeeId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId.Trim());
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var fullName = reader["FullName"] as string ?? string.Empty;
        SplitFullName(fullName, out var firstName, out var lastName);

        return new EmployeeTeacherLookupResult
        {
            EmployeeId = reader["EmployeeId"] as string ?? string.Empty,
            FullName = fullName,
            FirstName = firstName,
            LastName = lastName,
            Email = reader["Email"] as string,
            Phone = reader["Phone"] as string,
            Designation = reader["Designation"] as string,
            Qualification = reader["Qualification"] as string,
            Specialization = reader["Specialization"] as string,
            JoinedDate = reader.GetDateTime(reader.GetOrdinal("JoinedDate")),
            Status = reader["Status"] as string ?? string.Empty
        };
    }

    public async Task<bool> CnicExistsAsync(string cnic, int? excludeUid = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Employee
            WHERE CNIC = @CNIC
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CNIC", cnic.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static void AddWriteParameters(SqlCommand command, EmployeeFormModel model)
    {
        command.Parameters.AddWithValue("@EmployeeId", model.EmployeeId.Trim());
        command.Parameters.AddWithValue("@FullName", model.FullName.Trim());
        command.Parameters.AddWithValue("@Email", model.Email.Trim());
        command.Parameters.AddWithValue("@Phone", (object?)model.Phone?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@CNIC", model.CNIC.Trim());
        command.Parameters.AddWithValue("@FatherName", (object?)model.FatherName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@DOB", model.DOB.HasValue ? model.DOB.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@Department", (object?)model.Department?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Designation", (object?)model.Designation?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Specialization", (object?)model.Specialization?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Qualification", (object?)model.Qualification?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@EmployeeType", (object?)model.EmployeeType?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@JoinedDate", model.JoinedDate!.Value.Date);
        command.Parameters.AddWithValue("@Notes", (object?)model.Notes?.Trim() ?? DBNull.Value);
    }

    private static EmployeeFormModel MapRow(SqlDataReader reader)
    {
        return new EmployeeFormModel
        {
            Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
            EmployeeId = reader["EmployeeId"] as string ?? string.Empty,
            FullName = reader["FullName"] as string ?? string.Empty,
            Email = reader["Email"] as string ?? string.Empty,
            Phone = reader["Phone"] as string,
            CNIC = reader["CNIC"] as string ?? string.Empty,
            FatherName = reader["FatherName"] as string,
            DOB = reader.IsDBNull(reader.GetOrdinal("DOB")) ? null : reader.GetDateTime(reader.GetOrdinal("DOB")),
            Department = reader["Department"] as string,
            Designation = reader["Designation"] as string,
            Specialization = reader["Specialization"] as string,
            Qualification = reader["Qualification"] as string,
            EmployeeType = reader["EmployeeType"] as string,
            Status = reader["Status"] as string ?? string.Empty,
            JoinedDate = reader.GetDateTime(reader.GetOrdinal("JoinedDate")),
            Notes = reader["Notes"] as string
        };
    }

    private static void SplitFullName(string fullName, out string firstName, out string lastName)
    {
        var trimmed = fullName.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex <= 0)
        {
            firstName = trimmed;
            lastName = string.Empty;
            return;
        }

        firstName = trimmed[..spaceIndex].Trim();
        lastName = trimmed[(spaceIndex + 1)..].Trim();
    }
}
