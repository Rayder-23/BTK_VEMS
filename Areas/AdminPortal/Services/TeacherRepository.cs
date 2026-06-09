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
                t.Uid,
                t.EmployeeCode,
                t.FirstName,
                t.LastName,
                t.Designation,
                p.ProgramName,
                t.Email,
                t.Phone,
                t.IsActive
            FROM dbo.Teachers t
            LEFT JOIN dbo.ref_Programs p ON t.ProgramID = p.Uid
            WHERE (@Search IS NULL
                   OR t.EmployeeCode LIKE @Search
                   OR t.FirstName LIKE @Search
                   OR t.LastName LIKE @Search
                   OR t.Email LIKE @Search
                   OR t.Designation LIKE @Search
                   OR p.ProgramName LIKE @Search)
            """ + (activeOnly ? " AND t.IsActive = 1" : "") + """
             ORDER BY t.EmployeeCode;
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
                Uid = Convert.ToInt32(reader["Uid"]),
                EmployeeCode = reader["EmployeeCode"] as string ?? string.Empty,
                FirstName = reader["FirstName"] as string ?? string.Empty,
                LastName = reader["LastName"] as string ?? string.Empty,
                Designation = reader["Designation"] as string,
                ProgramName = reader["ProgramName"] as string,
                Email = reader["Email"] as string,
                Phone = reader["Phone"] as string,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<TeacherFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                EmployeeCode,
                FirstName,
                LastName,
                Designation,
                Qualification,
                Specialization,
                ProgramID,
                Email,
                Phone,
                JoiningDate,
                IsActive,
                Remarks
            FROM dbo.Teachers
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

        return Map(reader);
    }

    public async Task<TeacherLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var programs = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            programs.Add(new StudentLookupItem
            {
                Id = Convert.ToInt32(reader[0]),
                Name = reader[1] as string ?? string.Empty
            });
        }

        return new TeacherLookups { Programs = programs };
    }

    public async Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Teachers
            WHERE EmployeeCode = @EmployeeCode
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<bool> EmailExistsAsync(string? email, int? excludeUid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Teachers
            WHERE Email = @Email
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(TeacherFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Teachers (
                EmployeeCode,
                FirstName,
                LastName,
                Designation,
                Qualification,
                Specialization,
                ProgramID,
                Email,
                Phone,
                JoiningDate,
                IsActive,
                Remarks,
                CreatedBy,
                CreatedAt
            )
            VALUES (
                @EmployeeCode,
                @FirstName,
                @LastName,
                @Designation,
                @Qualification,
                @Specialization,
                @ProgramID,
                @Email,
                @Phone,
                @JoiningDate,
                @IsActive,
                @Remarks,
                @CreatedBy,
                SYSUTCDATETIME()
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(TeacherFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Teachers SET
                EmployeeCode = @EmployeeCode,
                FirstName = @FirstName,
                LastName = @LastName,
                Designation = @Designation,
                Qualification = @Qualification,
                Specialization = @Specialization,
                ProgramID = @ProgramID,
                Email = @Email,
                Phone = @Phone,
                JoiningDate = @JoiningDate,
                IsActive = @IsActive,
                Remarks = @Remarks,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Teachers SET
                IsActive = 0,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static TeacherFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        EmployeeCode = reader["EmployeeCode"] as string ?? string.Empty,
        FirstName = reader["FirstName"] as string ?? string.Empty,
        LastName = reader["LastName"] as string ?? string.Empty,
        Designation = reader["Designation"] as string,
        Qualification = reader["Qualification"] as string,
        Specialization = reader["Specialization"] as string,
        ProgramId = reader["ProgramID"] is DBNull ? null : Convert.ToInt32(reader["ProgramID"]),
        Email = reader["Email"] as string,
        Phone = reader["Phone"] as string,
        JoiningDate = reader["JoiningDate"] is DBNull ? null : reader.GetDateTime(reader.GetOrdinal("JoiningDate")),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        Remarks = reader["Remarks"] as string
    };

    private static void Bind(SqlCommand command, TeacherFormModel model)
    {
        command.Parameters.AddWithValue("@EmployeeCode", model.EmployeeCode.Trim());
        command.Parameters.AddWithValue("@FirstName", model.FirstName.Trim());
        command.Parameters.AddWithValue("@LastName", model.LastName.Trim());
        command.Parameters.AddWithValue("@Designation", (object?)model.Designation?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Qualification", (object?)model.Qualification?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Specialization", (object?)model.Specialization?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProgramID", (object?)model.ProgramId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)model.Email?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)model.Phone?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@JoiningDate", model.JoiningDate.HasValue ? model.JoiningDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
    }
}
