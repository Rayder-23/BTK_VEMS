using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeacherRepository : ITeacherRepository
{
    private readonly string _connectionString;

    public TeacherRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<TeacherListItemViewModel>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                t.TeacherID,
                t.EmployeeNo,
                t.TeacherName,
                t.Email,
                t.MobileNo,
                t.IsActive
            FROM dbo.Teachers t
            WHERE (@Search IS NULL
                   OR t.EmployeeNo LIKE @Search
                   OR t.TeacherName LIKE @Search
                   OR t.Email LIKE @Search
                   OR t.MobileNo LIKE @Search)
            """ + (activeOnly ? " AND t.IsActive = 1" : "") + """
             ORDER BY t.TeacherName, t.EmployeeNo;
            """;

        var list = new List<TeacherListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TeacherListItemViewModel
            {
                TeacherId = Convert.ToInt32(reader["TeacherID"]),
                EmployeeNo = reader["EmployeeNo"] as string ?? string.Empty,
                TeacherName = reader["TeacherName"] as string ?? string.Empty,
                Email = reader["Email"] as string,
                MobileNo = reader["MobileNo"] as string,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<TeacherFormModel?> GetAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                TeacherID,
                EmployeeNo,
                TeacherName,
                Email,
                MobileNo,
                IsActive
            FROM dbo.Teachers
            WHERE TeacherID = @TeacherId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<bool> EmployeeNoExistsAsync(string? employeeNo, int? excludeTeacherId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeNo))
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Teachers
            WHERE EmployeeNo = @EmployeeNo
              AND (@ExcludeTeacherId IS NULL OR TeacherID <> @ExcludeTeacherId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeNo", employeeNo.Trim());
        command.Parameters.AddWithValue("@ExcludeTeacherId", (object?)excludeTeacherId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> EmailExistsAsync(string? email, int? excludeTeacherId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Teachers
            WHERE Email = @Email
              AND (@ExcludeTeacherId IS NULL OR TeacherID <> @ExcludeTeacherId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim());
        command.Parameters.AddWithValue("@ExcludeTeacherId", (object?)excludeTeacherId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(TeacherFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Teachers (
                EmployeeNo,
                TeacherName,
                Email,
                MobileNo,
                IsActive
            )
            VALUES (
                @EmployeeNo,
                @TeacherName,
                @Email,
                @MobileNo,
                @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(TeacherFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Teachers SET
                EmployeeNo = @EmployeeNo,
                TeacherName = @TeacherName,
                Email = @Email,
                MobileNo = @MobileNo,
                IsActive = @IsActive
            WHERE TeacherID = @TeacherId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", model.TeacherId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int teacherId, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Teachers SET
                IsActive = 0
            WHERE TeacherID = @TeacherId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TeacherId", teacherId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static TeacherFormModel Map(SqlDataReader reader) => new()
    {
        TeacherId = Convert.ToInt32(reader["TeacherID"]),
        EmployeeNo = reader["EmployeeNo"] as string,
        TeacherName = reader["TeacherName"] as string ?? string.Empty,
        Email = reader["Email"] as string,
        MobileNo = reader["MobileNo"] as string,
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static void Bind(SqlCommand command, TeacherFormModel model)
    {
        command.Parameters.AddWithValue("@EmployeeNo", string.IsNullOrWhiteSpace(model.EmployeeNo) ? DBNull.Value : model.EmployeeNo.Trim());
        command.Parameters.AddWithValue("@TeacherName", model.TeacherName.Trim());
        command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(model.Email) ? DBNull.Value : model.Email.Trim());
        command.Parameters.AddWithValue("@MobileNo", string.IsNullOrWhiteSpace(model.MobileNo) ? DBNull.Value : model.MobileNo.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
