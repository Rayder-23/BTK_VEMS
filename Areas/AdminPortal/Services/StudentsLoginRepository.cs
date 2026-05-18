using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentsLoginRepository : IStudentsLoginRepository
{
    private readonly string _connectionString;

    public StudentsLoginRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<StudentLoginListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                sl.Uid,
                sl.StudentId,
                sl.Username,
                sl.Email,
                sl.Status,
                sl.LastLoginAt,
                sl.MustChangePassword,
                s.RegistrationNo,
                s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName AS StudentName
            FROM dbo.StudentsLogin sl
            INNER JOIN dbo.Students s ON sl.StudentId = s.Uid
            ORDER BY sl.Uid DESC;
            """;

        var list = new List<StudentLoginListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentLoginListItemViewModel
            {
                Uid = ToInt32(reader, "Uid"),
                StudentId = ToInt32(reader, "StudentId"),
                Username = reader["Username"] as string ?? string.Empty,
                Email = reader["Email"] as string,
                Status = reader["Status"] as string ?? string.Empty,
                StudentName = reader["StudentName"] as string ?? string.Empty,
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                LastLoginAt = reader["LastLoginAt"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("LastLoginAt")),
                MustChangePassword = reader.GetBoolean(reader.GetOrdinal("MustChangePassword"))
            });
        }

        return list;
    }

    public async Task<StudentLoginFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                sl.Uid,
                sl.StudentId,
                sl.Username,
                sl.Email,
                sl.Status,
                sl.MustChangePassword,
                s.RegistrationNo,
                s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName AS StudentName
            FROM dbo.StudentsLogin sl
            INNER JOIN dbo.Students s ON sl.StudentId = s.Uid
            WHERE sl.Uid = @Uid;
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

    public async Task<IReadOnlyList<StudentLookupItem>> GetStudentsWithoutLoginAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.Uid,
                s.RegistrationNo + ' — ' + s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName AS DisplayName
            FROM dbo.Students s
            WHERE NOT EXISTS (SELECT 1 FROM dbo.StudentsLogin sl WHERE sl.StudentId = s.Uid)
            ORDER BY s.RegistrationNo;
            """;

        return await ReadStudentLookupAsync(sql, cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUid = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.StudentsLogin
            WHERE Username = @Username
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Username", username.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    public async Task<int> InsertAsync(StudentLoginFormModel model, string plainPassword, int? createdBy, CancellationToken cancellationToken = default)
    {
        var hash = StudentPasswordHasher.HashPassword(plainPassword);

        const string sql = """
            INSERT INTO dbo.StudentsLogin (
                StudentId, Username, PasswordHash, CreatedBy, CreatedOn, Status, Email,
                FailedLoginCount, MustChangePassword, PasswordChangedAt
            )
            VALUES (
                @StudentId, @Username, @PasswordHash, @CreatedBy, SYSUTCDATETIME(), @Status, @Email,
                0, @MustChangePassword, SYSUTCDATETIME()
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        AddWriteParameters(command, model, hash, createdBy);
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(StudentLoginFormModel model, string? plainPassword, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var updatePassword = !string.IsNullOrWhiteSpace(plainPassword);
        var hash = updatePassword ? StudentPasswordHasher.HashPassword(plainPassword!) : string.Empty;

        const string sql = """
            UPDATE dbo.StudentsLogin
            SET Username = @Username,
                Email = @Email,
                Status = @Status,
                MustChangePassword = @MustChangePassword,
                PasswordHash = CASE WHEN @UpdatePassword = 1 THEN @PasswordHash ELSE PasswordHash END,
                PasswordChangedAt = CASE WHEN @UpdatePassword = 1 THEN SYSUTCDATETIME() ELSE PasswordChangedAt END,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        command.Parameters.AddWithValue("@Username", model.Username.Trim());
        command.Parameters.AddWithValue("@Email", (object?)model.Email?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@MustChangePassword", model.MustChangePassword);
        command.Parameters.AddWithValue("@UpdatePassword", updatePassword);
        command.Parameters.AddWithValue("@PasswordHash", hash);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static void AddWriteParameters(SqlCommand command, StudentLoginFormModel model, string passwordHash, int? createdBy)
    {
        command.Parameters.AddWithValue("@StudentId", model.StudentId);
        command.Parameters.AddWithValue("@Username", model.Username.Trim());
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@Email", (object?)model.Email?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@MustChangePassword", model.MustChangePassword);
    }

    private static StudentLoginFormModel MapForm(SqlDataReader reader) =>
        new()
        {
            Uid = ToInt32(reader, "Uid"),
            StudentId = ToInt32(reader, "StudentId"),
            Username = reader["Username"] as string ?? string.Empty,
            Email = reader["Email"] as string,
            Status = reader["Status"] as string ?? "Active",
            MustChangePassword = reader.GetBoolean(reader.GetOrdinal("MustChangePassword")),
            RegistrationNo = reader["RegistrationNo"] as string,
            StudentDisplayName = reader["StudentName"] as string
        };

    private async Task<IReadOnlyList<StudentLookupItem>> ReadStudentLookupAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentLookupItem
            {
                Id = ToInt32(reader, 0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    private static int ToInt32(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(column)));

    private static int ToInt32(SqlDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal));
}
