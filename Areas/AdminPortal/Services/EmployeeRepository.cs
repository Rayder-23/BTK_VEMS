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
                uid,
                EmployeeID,
                EmployeeName,
                Department,
                Designation,
                EmployeeStatus
            FROM dbo.Employee
            ORDER BY uid DESC;
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
                Uid = reader.GetInt32(reader.GetOrdinal("uid")),
                EmployeeID = reader["EmployeeID"] as string ?? string.Empty,
                EmployeeName = reader["EmployeeName"] as string,
                Department = reader["Department"] as string,
                Designation = reader["Designation"] as string,
                EmployeeStatus = reader["EmployeeStatus"] as string
            });
        }

        return list;
    }

    public async Task<EmployeeFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                uid,
                EmployeeID,
                EmployeeName,
                CNIC,
                FatherName,
                DOB,
                MobileNo,
                Department,
                Designation,
                DateOfJoining,
                EmployeeStatus,
                ModifiedBy,
                ModifiedOn,
                Details,
                Project,
                CarryForwardLeaves,
                Year2022,
                Year2023,
                AdjustedAjusted,
                Year2024,
                CarryForwardLeaves1,
                Year2023New,
                BasicSalary,
                ApplyTax,
                GenStatus
            FROM dbo.Employee
            WHERE uid = @uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@uid", uid);
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
        const string sql = """
            INSERT INTO dbo.Employee (
                EmployeeID,
                EmployeeName,
                CNIC,
                FatherName,
                DOB,
                MobileNo,
                Department,
                Designation,
                DateOfJoining,
                EmployeeStatus,
                ModifiedBy,
                ModifiedOn,
                Details,
                Project,
                CarryForwardLeaves,
                Year2022,
                Year2023,
                AdjustedAjusted,
                Year2024,
                CarryForwardLeaves1,
                Year2023New,
                BasicSalary,
                ApplyTax,
                GenStatus
            )
            OUTPUT INSERTED.uid
            VALUES (
                @EmployeeID,
                @EmployeeName,
                @CNIC,
                @FatherName,
                @DOB,
                @MobileNo,
                @Department,
                @Designation,
                @DateOfJoining,
                @EmployeeStatus,
                @ModifiedBy,
                @ModifiedOn,
                @Details,
                @Project,
                @CarryForwardLeaves,
                @Year2022,
                @Year2023,
                @AdjustedAjusted,
                @Year2024,
                @CarryForwardLeaves1,
                @Year2023New,
                @BasicSalary,
                @ApplyTax,
                @GenStatus
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddWriteParameters(command, model);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(EmployeeFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Employee
            SET
                EmployeeID = @EmployeeID,
                EmployeeName = @EmployeeName,
                CNIC = @CNIC,
                FatherName = @FatherName,
                DOB = @DOB,
                MobileNo = @MobileNo,
                Department = @Department,
                Designation = @Designation,
                DateOfJoining = @DateOfJoining,
                EmployeeStatus = @EmployeeStatus,
                ModifiedBy = @ModifiedBy,
                ModifiedOn = @ModifiedOn,
                Details = @Details,
                Project = @Project,
                CarryForwardLeaves = @CarryForwardLeaves,
                Year2022 = @Year2022,
                Year2023 = @Year2023,
                AdjustedAjusted = @AdjustedAjusted,
                Year2024 = @Year2024,
                CarryForwardLeaves1 = @CarryForwardLeaves1,
                Year2023New = @Year2023New,
                BasicSalary = @BasicSalary,
                ApplyTax = @ApplyTax,
                GenStatus = @GenStatus
            WHERE uid = @uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@uid", model.Uid);
        AddWriteParameters(command, model);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Employee WHERE uid = @uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static void AddWriteParameters(SqlCommand command, EmployeeFormModel model)
    {
        command.Parameters.AddWithValue("@EmployeeID", model.EmployeeID.Trim());
        command.Parameters.AddWithValue("@EmployeeName", (object?)model.EmployeeName ?? DBNull.Value);
        command.Parameters.AddWithValue("@CNIC", (object?)model.CNIC ?? DBNull.Value);
        command.Parameters.AddWithValue("@FatherName", (object?)model.FatherName ?? DBNull.Value);
        command.Parameters.AddWithValue("@DOB", (object?)model.DOB ?? DBNull.Value);
        command.Parameters.AddWithValue("@MobileNo", (object?)model.MobileNo ?? DBNull.Value);
        command.Parameters.AddWithValue("@Department", (object?)model.Department ?? DBNull.Value);
        command.Parameters.AddWithValue("@Designation", (object?)model.Designation ?? DBNull.Value);
        command.Parameters.AddWithValue("@DateOfJoining", (object?)model.DateOfJoining ?? DBNull.Value);
        command.Parameters.AddWithValue("@EmployeeStatus", (object?)model.EmployeeStatus ?? DBNull.Value);
        command.Parameters.AddWithValue("@ModifiedBy", (object?)model.ModifiedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@ModifiedOn", (object?)model.ModifiedOn ?? DBNull.Value);
        command.Parameters.AddWithValue("@Details", (object?)model.Details ?? DBNull.Value);
        command.Parameters.AddWithValue("@Project", (object?)model.Project ?? DBNull.Value);
        command.Parameters.AddWithValue("@CarryForwardLeaves", (object?)model.CarryForwardLeaves ?? DBNull.Value);
        command.Parameters.AddWithValue("@Year2022", (object?)model.Year2022 ?? DBNull.Value);
        command.Parameters.AddWithValue("@Year2023", (object?)model.Year2023 ?? DBNull.Value);
        command.Parameters.AddWithValue("@AdjustedAjusted", (object?)model.AdjustedAjusted ?? DBNull.Value);
        command.Parameters.AddWithValue("@Year2024", (object?)model.Year2024 ?? DBNull.Value);
        command.Parameters.AddWithValue("@CarryForwardLeaves1", (object?)model.CarryForwardLeaves1 ?? DBNull.Value);
        command.Parameters.AddWithValue("@Year2023New", (object?)model.Year2023New ?? DBNull.Value);
        command.Parameters.AddWithValue("@BasicSalary", (object?)model.BasicSalary ?? DBNull.Value);
        command.Parameters.AddWithValue("@ApplyTax", (object?)model.ApplyTax ?? DBNull.Value);
        command.Parameters.AddWithValue("@GenStatus", (object?)model.GenStatus ?? DBNull.Value);
    }

    private static EmployeeFormModel MapRow(SqlDataReader reader)
    {
        static string? S(SqlDataReader r, string name) => r[name] as string;

        return new EmployeeFormModel
        {
            Uid = reader.GetInt32(reader.GetOrdinal("uid")),
            EmployeeID = reader["EmployeeID"] as string ?? string.Empty,
            EmployeeName = S(reader, "EmployeeName"),
            CNIC = S(reader, "CNIC"),
            FatherName = S(reader, "FatherName"),
            DOB = S(reader, "DOB"),
            MobileNo = S(reader, "MobileNo"),
            Department = S(reader, "Department"),
            Designation = S(reader, "Designation"),
            DateOfJoining = reader.IsDBNull(reader.GetOrdinal("DateOfJoining"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("DateOfJoining")),
            EmployeeStatus = S(reader, "EmployeeStatus"),
            ModifiedBy = S(reader, "ModifiedBy"),
            ModifiedOn = S(reader, "ModifiedOn"),
            Details = S(reader, "Details"),
            Project = S(reader, "Project"),
            CarryForwardLeaves = reader.IsDBNull(reader.GetOrdinal("CarryForwardLeaves"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("CarryForwardLeaves")),
            Year2022 = reader.IsDBNull(reader.GetOrdinal("Year2022")) ? null : reader.GetDouble(reader.GetOrdinal("Year2022")),
            Year2023 = reader.IsDBNull(reader.GetOrdinal("Year2023")) ? null : reader.GetDouble(reader.GetOrdinal("Year2023")),
            AdjustedAjusted = reader.IsDBNull(reader.GetOrdinal("AdjustedAjusted"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("AdjustedAjusted")),
            Year2024 = reader.IsDBNull(reader.GetOrdinal("Year2024")) ? null : reader.GetInt32(reader.GetOrdinal("Year2024")),
            CarryForwardLeaves1 = reader.IsDBNull(reader.GetOrdinal("CarryForwardLeaves1"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("CarryForwardLeaves1")),
            Year2023New = reader.IsDBNull(reader.GetOrdinal("Year2023New"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("Year2023New")),
            BasicSalary = reader.IsDBNull(reader.GetOrdinal("BasicSalary"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("BasicSalary")),
            ApplyTax = S(reader, "ApplyTax"),
            GenStatus = S(reader, "GenStatus")
        };
    }
}
