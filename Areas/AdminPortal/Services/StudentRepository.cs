using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentRepository : IStudentRepository
{
    private readonly string _connectionString;

    public StudentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<StudentListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                FullName,
                Email,
                Phone,
                GuardianName,
                GradeLevel,
                Status,
                EnrolledDate
            FROM dbo.Students
            ORDER BY Uid DESC;
            """;

        var list = new List<StudentListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentListItemViewModel
            {
                Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
                FullName = reader["FullName"] as string ?? string.Empty,
                Email = reader["Email"] as string,
                Phone = reader["Phone"] as string,
                GuardianName = reader["GuardianName"] as string ?? string.Empty,
                GradeLevel = reader["GradeLevel"] as string ?? string.Empty,
                Status = reader["Status"] as string ?? string.Empty,
                EnrolledDate = reader.GetDateTime(reader.GetOrdinal("EnrolledDate"))
            });
        }

        return list;
    }

    public async Task<StudentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                FullName,
                Email,
                Phone,
                GuardianName,
                GuardianPhone,
                GradeLevel,
                City,
                Status,
                EnrolledDate
            FROM dbo.Students
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

        return new StudentFormModel
        {
            Uid = reader.GetInt32(reader.GetOrdinal("Uid")),
            FullName = reader["FullName"] as string ?? string.Empty,
            Email = reader["Email"] as string,
            Phone = reader["Phone"] as string,
            GuardianName = reader["GuardianName"] as string ?? string.Empty,
            GuardianPhone = reader["GuardianPhone"] as string ?? string.Empty,
            GradeLevel = reader["GradeLevel"] as string ?? string.Empty,
            City = reader["City"] as string,
            Status = reader["Status"] as string ?? string.Empty,
            EnrolledDate = reader.GetDateTime(reader.GetOrdinal("EnrolledDate"))
        };
    }

    public async Task<int> InsertAsync(StudentFormModel model, CancellationToken cancellationToken = default)
    {
        if (!model.EnrolledDate.HasValue)
        {
            throw new ArgumentException("EnrolledDate is required.", nameof(model));
        }

        const string sql = """
            INSERT INTO dbo.Students (
                FullName,
                Email,
                Phone,
                GuardianName,
                GuardianPhone,
                GradeLevel,
                City,
                Status,
                EnrolledDate
            )
            OUTPUT INSERTED.Uid
            VALUES (
                @FullName,
                @Email,
                @Phone,
                @GuardianName,
                @GuardianPhone,
                @GradeLevel,
                @City,
                @Status,
                @EnrolledDate
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddWriteParameters(command, model);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(StudentFormModel model, CancellationToken cancellationToken = default)
    {
        if (!model.EnrolledDate.HasValue)
        {
            throw new ArgumentException("EnrolledDate is required.", nameof(model));
        }

        const string sql = """
            UPDATE dbo.Students
            SET
                FullName = @FullName,
                Email = @Email,
                Phone = @Phone,
                GuardianName = @GuardianName,
                GuardianPhone = @GuardianPhone,
                GradeLevel = @GradeLevel,
                City = @City,
                Status = @Status,
                EnrolledDate = @EnrolledDate
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        AddWriteParameters(command, model);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Students WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static void AddWriteParameters(SqlCommand command, StudentFormModel model)
    {
        command.Parameters.AddWithValue("@FullName", model.FullName.Trim());
        command.Parameters.AddWithValue("@Email", (object?)model.Email?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)model.Phone?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@GuardianName", model.GuardianName.Trim());
        command.Parameters.AddWithValue("@GuardianPhone", model.GuardianPhone.Trim());
        command.Parameters.AddWithValue("@GradeLevel", model.GradeLevel.Trim());
        command.Parameters.AddWithValue("@City", (object?)model.City?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@EnrolledDate", model.EnrolledDate!.Value.Date);
    }
}
