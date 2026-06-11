using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class CourseRepository : ICourseRepository
{
    private readonly string _connectionString;

    public CourseRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<CourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        int? programId = null,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                c.Uid,
                c.CourseCode,
                c.CourseTitle,
                p.ProgramName,
                c.ShortName,
                c.CreditHours,
                c.SemesterNo,
                c.IsMandatory,
                c.IsActive,
                c.CreatedAt
            FROM dbo.Courses c
            INNER JOIN dbo.ref_Programs p ON c.ProgramID = p.Uid
            WHERE (@Search IS NULL
                   OR c.CourseCode LIKE @Search
                   OR c.CourseTitle LIKE @Search
                   OR c.ShortName LIKE @Search
                   OR p.ProgramName LIKE @Search)
              AND (@ProgramId IS NULL OR c.ProgramID = @ProgramId)
            """ + (activeOnly ? " AND c.IsActive = 1" : "") + """
             ORDER BY c.CourseCode;
            """;

        var list = new List<CourseListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        command.Parameters.AddWithValue("@ProgramId", (object?)programId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapListItem(reader));
        }

        return list;
    }

    public async Task<CourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ProgramID,
                CourseCode,
                CourseTitle,
                ShortName,
                CreditHours,
                SemesterNo,
                IsMandatory,
                IsActive,
                CreatedAt
            FROM dbo.Courses
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<CourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string programsSql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var programs = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(programsSql, connection);
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

        return new CourseLookups { Programs = programs };
    }

    public async Task<bool> CourseCodeExistsAsync(string courseCode, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Courses
            WHERE CourseCode = @CourseCode
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseCode", courseCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(CourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Courses (
                ProgramID,
                CourseCode,
                CourseTitle,
                ShortName,
                CreditHours,
                SemesterNo,
                IsMandatory,
                IsActive
            )
            VALUES (
                @ProgramID,
                @CourseCode,
                @CourseTitle,
                @ShortName,
                @CreditHours,
                @SemesterNo,
                @IsMandatory,
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

    public async Task<bool> UpdateAsync(CourseFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Courses SET
                ProgramID = @ProgramID,
                CourseCode = @CourseCode,
                CourseTitle = @CourseTitle,
                ShortName = @ShortName,
                CreditHours = @CreditHours,
                SemesterNo = @SemesterNo,
                IsMandatory = @IsMandatory,
                IsActive = @IsActive
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Courses SET IsActive = 0
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static CourseListItem MapListItem(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        CourseCode = reader["CourseCode"] as string ?? string.Empty,
        CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        CreditHours = Convert.ToByte(reader["CreditHours"]),
        SemesterNo = reader["SemesterNo"] is DBNull ? null : Convert.ToByte(reader["SemesterNo"]),
        IsMandatory = Convert.ToBoolean(reader["IsMandatory"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static CourseFormModel MapForm(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        CourseCode = reader["CourseCode"] as string ?? string.Empty,
        CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        CreditHours = Convert.ToByte(reader["CreditHours"]),
        SemesterNo = reader["SemesterNo"] is DBNull ? null : Convert.ToByte(reader["SemesterNo"]),
        IsMandatory = Convert.ToBoolean(reader["IsMandatory"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static void Bind(SqlCommand command, CourseFormModel model)
    {
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@CourseCode", model.CourseCode.Trim());
        command.Parameters.AddWithValue("@CourseTitle", model.CourseTitle.Trim());
        command.Parameters.AddWithValue("@ShortName", (object?)model.ShortName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreditHours", model.CreditHours);
        command.Parameters.AddWithValue("@SemesterNo", (object?)model.SemesterNo ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsMandatory", model.IsMandatory);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
