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
                c.IsActive
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
            list.Add(new ClassListItem
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
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<ClassFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                ClassName,
                ClassCode,
                ProgramID,
                SemesterNo,
                Semester,
                AcademicYear,
                Section,
                Shift,
                RoomNo,
                MaxStrength,
                IsActive,
                Remarks
            FROM dbo.Classes
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

    public async Task<int> InsertAsync(ClassFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Classes (
                ClassName,
                ClassCode,
                ProgramID,
                SemesterNo,
                Semester,
                AcademicYear,
                Section,
                Shift,
                RoomNo,
                MaxStrength,
                IsActive,
                Remarks,
                CreatedBy,
                CreatedAt
            )
            VALUES (
                @ClassName,
                @ClassCode,
                @ProgramID,
                @SemesterNo,
                @Semester,
                @AcademicYear,
                @Section,
                @Shift,
                @RoomNo,
                @MaxStrength,
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

    public async Task<bool> UpdateAsync(ClassFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Classes SET
                ClassName = @ClassName,
                ClassCode = @ClassCode,
                ProgramID = @ProgramID,
                SemesterNo = @SemesterNo,
                Semester = @Semester,
                AcademicYear = @AcademicYear,
                Section = @Section,
                Shift = @Shift,
                RoomNo = @RoomNo,
                MaxStrength = @MaxStrength,
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
            UPDATE dbo.Classes SET
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

    private static ClassFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        ClassName = reader["ClassName"] as string ?? string.Empty,
        ClassCode = reader["ClassCode"] as string ?? string.Empty,
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        SemesterNo = Convert.ToByte(reader["SemesterNo"]),
        Semester = reader["Semester"] as string ?? string.Empty,
        AcademicYear = Convert.ToInt16(reader["AcademicYear"]),
        Section = reader["Section"] as string,
        Shift = reader["Shift"] as string,
        RoomNo = reader["RoomNo"] as string,
        MaxStrength = Convert.ToInt16(reader["MaxStrength"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        Remarks = reader["Remarks"] as string
    };

    private static void Bind(SqlCommand command, ClassFormModel model)
    {
        command.Parameters.AddWithValue("@ClassName", model.ClassName.Trim());
        command.Parameters.AddWithValue("@ClassCode", model.ClassCode.Trim());
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@SemesterNo", model.SemesterNo);
        command.Parameters.AddWithValue("@Semester", model.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@Section", (object?)model.Section?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Shift", (object?)model.Shift?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@RoomNo", (object?)model.RoomNo?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@MaxStrength", model.MaxStrength);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
    }
}
