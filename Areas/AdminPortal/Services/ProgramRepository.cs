using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ProgramRepository : IProgramRepository
{
    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public ProgramRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<ProgramListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                p.Uid,
                p.ProgramCode,
                p.ProgramName,
                it.InstTypeName,
                p.DegreeLevel,
                p.Status,
                p.IsActive
            FROM dbo.ref_Programs p
            INNER JOIN dbo.ref_InstitutionTypes it ON p.InstTypeId = it.Uid
            WHERE (@Search IS NULL
                   OR p.ProgramCode LIKE @Search
                   OR p.ProgramName LIKE @Search
                   OR p.ShortName LIKE @Search
                   OR it.InstTypeName LIKE @Search)
            """ + (activeOnly ? " AND p.IsActive = 1" : "") + """
             ORDER BY p.ProgramCode;
            """;

        var list = new List<ProgramListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ProgramListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                InstitutionTypeName = reader["InstTypeName"] as string ?? string.Empty,
                DegreeLevel = reader["DegreeLevel"] as string,
                Status = reader["Status"] as string ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<ProgramListItem?> GetListItemAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                p.Uid,
                p.ProgramCode,
                p.ProgramName,
                it.InstTypeName,
                p.DegreeLevel,
                p.Status,
                p.IsActive
            FROM dbo.ref_Programs p
            INNER JOIN dbo.ref_InstitutionTypes it ON p.InstTypeId = it.Uid
            WHERE p.Uid = @Uid;
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

        return new ProgramListItem
        {
            Uid = Convert.ToInt32(reader["Uid"]),
            ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
            ProgramName = reader["ProgramName"] as string ?? string.Empty,
            InstitutionTypeName = reader["InstTypeName"] as string ?? string.Empty,
            DegreeLevel = reader["DegreeLevel"] as string,
            Status = reader["Status"] as string ?? string.Empty,
            IsActive = Convert.ToBoolean(reader["IsActive"])
        };
    }

    public async Task<IReadOnlyList<StudentLookupItem>> GetProgramOptionsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            """ + (activeOnly ? " WHERE IsActive = 1" : "") + """
             ORDER BY ProgramName;
            """;

        var list = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentLookupItem
            {
                Id = Convert.ToInt32(reader[0]),
                Name = reader[1] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<ProgramFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                InstTypeId,
                ProgramCode,
                ProgramName,
                ShortName,
                ProgramLevel,
                ProgramType,
                DegreeLevel,
                DurationYears,
                TotalSemesters,
                TotalGrades,
                TotalCreditHours,
                Status,
                IsActive
            FROM dbo.ref_Programs
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

    public async Task<ProgramLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string instTypesSql = """
            SELECT Uid, InstTypeCode + ' - ' + InstTypeName
            FROM dbo.ref_InstitutionTypes
            WHERE IsActive = 1
            ORDER BY InstTypeName;
            """;

        var institutionTypes = new List<StudentLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var command = new SqlCommand(instTypesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                institutionTypes.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        return new ProgramLookups
        {
            InstitutionTypes = institutionTypes,
            ProgramLevels = await _configurations.GetValuesAsync("ProgramLevel", cancellationToken),
            ProgramTypes = await _configurations.GetValuesAsync("ProgramType", cancellationToken),
            DegreeLevels = await _configurations.GetValuesAsync("DegreeLevel", cancellationToken),
            ProgramStatuses = await _configurations.GetValuesAsync("ProgramStatus", cancellationToken)
        };
    }

    public async Task<bool> ProgramCodeExistsAsync(string programCode, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ref_Programs
            WHERE ProgramCode = @ProgramCode
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramCode", programCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ProgramFormModel model, int? createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ref_Programs (
                InstTypeId,
                ProgramCode,
                ProgramName,
                ShortName,
                ProgramLevel,
                ProgramType,
                DegreeLevel,
                DurationYears,
                TotalSemesters,
                TotalGrades,
                TotalCreditHours,
                Status,
                IsActive,
                CreatedBy,
                CreatedAt,
                UpdatedAt
            )
            VALUES (
                @InstTypeId,
                @ProgramCode,
                @ProgramName,
                @ShortName,
                @ProgramLevel,
                @ProgramType,
                @DegreeLevel,
                @DurationYears,
                @TotalSemesters,
                @TotalGrades,
                @TotalCreditHours,
                @Status,
                @IsActive,
                @CreatedBy,
                SYSUTCDATETIME(),
                SYSUTCDATETIME()
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        command.Parameters.AddWithValue("@CreatedBy", (object?)createdBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ProgramFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ref_Programs SET
                InstTypeId = @InstTypeId,
                ProgramCode = @ProgramCode,
                ProgramName = @ProgramName,
                ShortName = @ShortName,
                ProgramLevel = @ProgramLevel,
                ProgramType = @ProgramType,
                DegreeLevel = @DegreeLevel,
                DurationYears = @DurationYears,
                TotalSemesters = @TotalSemesters,
                TotalGrades = @TotalGrades,
                TotalCreditHours = @TotalCreditHours,
                Status = @Status,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
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
            UPDATE dbo.ref_Programs SET
                IsActive = 0,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ProgramFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        InstTypeId = Convert.ToInt32(reader["InstTypeId"]),
        ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        ProgramLevel = reader["ProgramLevel"] as string,
        ProgramType = reader["ProgramType"] as string,
        DegreeLevel = reader["DegreeLevel"] as string,
        DurationYears = reader["DurationYears"] is DBNull ? null : Convert.ToByte(reader["DurationYears"]),
        TotalSemesters = reader["TotalSemesters"] is DBNull ? null : Convert.ToByte(reader["TotalSemesters"]),
        TotalGrades = reader["TotalGrades"] is DBNull ? null : Convert.ToByte(reader["TotalGrades"]),
        TotalCreditHours = reader["TotalCreditHours"] is DBNull ? null : Convert.ToInt16(reader["TotalCreditHours"]),
        Status = reader["Status"] as string ?? string.Empty,
        IsActive = Convert.ToBoolean(reader["IsActive"])
    };

    private static void Bind(SqlCommand command, ProgramFormModel model)
    {
        command.Parameters.AddWithValue("@InstTypeId", model.InstTypeId);
        command.Parameters.AddWithValue("@ProgramCode", model.ProgramCode.Trim());
        command.Parameters.AddWithValue("@ProgramName", model.ProgramName.Trim());
        command.Parameters.AddWithValue("@ShortName", (object?)model.ShortName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProgramLevel", (object?)model.ProgramLevel?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProgramType", (object?)model.ProgramType?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@DegreeLevel", (object?)model.DegreeLevel?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@DurationYears", (object?)model.DurationYears ?? DBNull.Value);
        command.Parameters.AddWithValue("@TotalSemesters", (object?)model.TotalSemesters ?? DBNull.Value);
        command.Parameters.AddWithValue("@TotalGrades", (object?)model.TotalGrades ?? DBNull.Value);
        command.Parameters.AddWithValue("@TotalCreditHours", (object?)model.TotalCreditHours ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", model.Status.Trim());
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
