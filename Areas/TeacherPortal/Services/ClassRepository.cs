using Microsoft.Data.SqlClient;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.TeacherPortal.Services;

public sealed class ClassRepository : IClassRepository
{
    private const string SemesterConfigKey = "Semester";
    private const string ShiftConfigKey = "Shift";

    private static readonly string[] DefaultSemesters = ["Fall", "Spring", "Summer"];
    private static readonly string[] DefaultShifts = ["Morning", "Evening", "Weekend"];

    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public ClassRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<ClassListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                c.Uid,
                c.ClassName,
                c.ClassCode,
                p.ProgramName,
                c.SemesterNo,
                c.Semester,
                c.AcademicYear,
                c.Section,
                c.Shift,
                c.RoomNo,
                c.MaxStrength,
                c.IsActive,
                c.CreatedAt
            FROM dbo.Classes c
            INNER JOIN dbo.ref_Programs p ON c.ProgramID = p.Uid
            WHERE (@Search IS NULL
                   OR c.ClassName LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR p.ProgramName LIKE @Search
                   OR c.Section LIKE @Search)
            """ + (activeOnly ? " AND c.IsActive = 1" : "") + """
             ORDER BY c.AcademicYear DESC, c.ClassCode;
            """;

        var list = new List<ClassListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapListItem(reader));
        }

        return list;
    }

    public async Task<ClassFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ProgramID,
                ClassCode,
                ClassName,
                SemesterNo,
                Semester,
                AcademicYear,
                Section,
                Shift,
                RoomNo,
                MaxStrength,
                IsActive,
                CreatedAt
            FROM dbo.Classes
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<ClassLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string programsSql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var programs = new List<ClassLookupItem>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(programsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                programs.Add(new ClassLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        var semesters = await _configurations.GetValuesAsync(SemesterConfigKey, cancellationToken);
        var shifts = await _configurations.GetValuesAsync(ShiftConfigKey, cancellationToken);

        return new ClassLookups
        {
            Programs = programs,
            Semesters = semesters.Count > 0 ? semesters : DefaultSemesters,
            Shifts = shifts.Count > 0 ? shifts : DefaultShifts
        };
    }

    public async Task<bool> ClassCodeExistsAsync(string classCode, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Classes
            WHERE ClassCode = @ClassCode
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassCode", classCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ClassFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Classes (
                ProgramID,
                ClassCode,
                ClassName,
                SemesterNo,
                Semester,
                AcademicYear,
                Section,
                Shift,
                RoomNo,
                MaxStrength,
                IsActive
            )
            VALUES (
                @ProgramID,
                @ClassCode,
                @ClassName,
                @SemesterNo,
                @Semester,
                @AcademicYear,
                @Section,
                @Shift,
                @RoomNo,
                @MaxStrength,
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

    public async Task<bool> UpdateAsync(ClassFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Classes SET
                ProgramID = @ProgramID,
                ClassCode = @ClassCode,
                ClassName = @ClassName,
                SemesterNo = @SemesterNo,
                Semester = @Semester,
                AcademicYear = @AcademicYear,
                Section = @Section,
                Shift = @Shift,
                RoomNo = @RoomNo,
                MaxStrength = @MaxStrength,
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
            UPDATE dbo.Classes SET IsActive = 0
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static ClassListItem MapListItem(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ClassName = reader["ClassName"] as string ?? string.Empty,
        ClassCode = reader["ClassCode"] as string ?? string.Empty,
        ProgramName = reader["ProgramName"] as string ?? string.Empty,
        SemesterNo = Convert.ToByte(reader["SemesterNo"]),
        Semester = reader["Semester"] as string ?? string.Empty,
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        Section = reader["Section"] as string,
        Shift = reader["Shift"] as string,
        RoomNo = reader["RoomNo"] as string,
        MaxStrength = Convert.ToInt16(reader["MaxStrength"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static ClassFormModel MapForm(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        ClassCode = reader["ClassCode"] as string ?? string.Empty,
        ClassName = reader["ClassName"] as string ?? string.Empty,
        SemesterNo = Convert.ToByte(reader["SemesterNo"]),
        Semester = reader["Semester"] as string ?? string.Empty,
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        Section = reader["Section"] as string,
        Shift = reader["Shift"] as string,
        RoomNo = reader["RoomNo"] as string,
        MaxStrength = Convert.ToInt16(reader["MaxStrength"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
    };

    private static void Bind(SqlCommand command, ClassFormModel model)
    {
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@ClassCode", model.ClassCode.Trim());
        command.Parameters.AddWithValue("@ClassName", model.ClassName.Trim());
        command.Parameters.AddWithValue("@SemesterNo", model.SemesterNo);
        command.Parameters.AddWithValue("@Semester", model.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@Section", (object?)model.Section?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Shift", (object?)model.Shift?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@RoomNo", (object?)model.RoomNo?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@MaxStrength", model.MaxStrength);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
