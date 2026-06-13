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
                StudentID,
                RegistrationNo,
                StudentName,
                MobileNo,
                Email,
                CreatedOn,
                IsActive
            FROM dbo.Students
            ORDER BY StudentID DESC;
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
                StudentId = Convert.ToInt32(reader["StudentID"]),
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                StudentName = reader["StudentName"] as string ?? string.Empty,
                MobileNo = reader["MobileNo"] as string,
                Email = reader["Email"] as string,
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }

        return list;
    }

    public async Task<StudentFormModel?> GetAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                StudentID,
                RegistrationNo,
                StudentName,
                MobileNo,
                Email,
                IsActive
            FROM dbo.Students
            WHERE StudentID = @StudentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new StudentFormModel
        {
            StudentId = Convert.ToInt32(reader["StudentID"]),
            RegistrationNo = reader["RegistrationNo"] as string,
            StudentName = reader["StudentName"] as string ?? string.Empty,
            MobileNo = reader["MobileNo"] as string,
            Email = reader["Email"] as string,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }

    public async Task<int> InsertAsync(StudentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Students (
                RegistrationNo,
                StudentName,
                MobileNo,
                Email,
                IsActive
            )
            OUTPUT INSERTED.StudentID
            VALUES (
                @RegistrationNo,
                @StudentName,
                @MobileNo,
                @Email,
                @IsActive
            );
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(StudentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Students
            SET
                RegistrationNo = @RegistrationNo,
                StudentName = @StudentName,
                MobileNo = @MobileNo,
                Email = @Email,
                IsActive = @IsActive
            WHERE StudentID = @StudentId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", model.StudentId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int studentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Students WHERE StudentID = @StudentId;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, StudentFormModel model)
    {
        command.Parameters.AddWithValue("@RegistrationNo", string.IsNullOrWhiteSpace(model.RegistrationNo) ? DBNull.Value : model.RegistrationNo.Trim());
        command.Parameters.AddWithValue("@StudentName", model.StudentName.Trim());
        command.Parameters.AddWithValue("@MobileNo", string.IsNullOrWhiteSpace(model.MobileNo) ? DBNull.Value : model.MobileNo.Trim());
        command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(model.Email) ? DBNull.Value : model.Email.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
