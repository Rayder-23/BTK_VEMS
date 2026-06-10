using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class CourseRepository : ICourseRepository
{
    private readonly string _connectionString;
    private readonly IConfigurationValuesProvider _configurations;

    public CourseRepository(IConfiguration configuration, IConfigurationValuesProvider configurations)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
        _configurations = configurations;
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
                c.CreditHours,
                c.CourseType,
                c.CourseLevel,
                c.IsActive
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
            list.Add(new CourseListItem
            {
                Uid = Convert.ToInt32(reader["Uid"]),
                CourseCode = reader["CourseCode"] as string ?? string.Empty,
                CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string ?? string.Empty,
                CreditHours = Convert.ToByte(reader["CreditHours"]),
                CourseType = reader["CourseType"] as string ?? string.Empty,
                CourseLevel = reader["CourseLevel"] as string ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"])
            });
        }

        return list;
    }

    public async Task<CourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                CourseCode,
                CourseTitle,
                ShortName,
                ProgramID,
                CreditHours,
                TheoryHours,
                LabHours,
                CourseType,
                CourseLevel,
                SemesterNo,
                IsMandatory,
                IsActive,
                Description,
                Objectives,
                PrerequisiteCourseID
            FROM dbo.Courses
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

    public async Task<CourseLookups> GetLookupsAsync(int? excludeCourseUid, CancellationToken cancellationToken = default)
    {
        const string programsSql = """
            SELECT Uid, ProgramCode + ' - ' + ProgramName
            FROM dbo.ref_Programs
            WHERE IsActive = 1
            ORDER BY ProgramName;
            """;

        var programs = new List<StudentLookupItem>();
        var prerequisites = new List<StudentLookupItem>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = new SqlCommand(programsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                programs.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        var prerequisiteSql = """
            SELECT Uid, CourseCode + ' - ' + CourseTitle
            FROM dbo.Courses
            WHERE IsActive = 1
            """ + (excludeCourseUid.HasValue ? " AND Uid <> @ExcludeUid" : "") + """
             ORDER BY CourseCode;
            """;

        await using (var command = new SqlCommand(prerequisiteSql, connection))
        {
            if (excludeCourseUid.HasValue)
            {
                command.Parameters.AddWithValue("@ExcludeUid", excludeCourseUid.Value);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                prerequisites.Add(new StudentLookupItem
                {
                    Id = Convert.ToInt32(reader[0]),
                    Name = reader[1] as string ?? string.Empty
                });
            }
        }

        var courseTypes = CourseFieldCatalog.ResolveCourseTypes(
            await _configurations.GetValuesAsync("CourseType", cancellationToken));
        var courseLevels = await _configurations.GetValuesAsync("CourseLevel", cancellationToken);

        return new CourseLookups
        {
            Programs = programs,
            PrerequisiteCourses = prerequisites,
            CourseTypes = courseTypes,
            CourseLevels = courseLevels
        };
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

    public async Task<int> InsertAsync(CourseFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Courses (
                CourseCode,
                CourseTitle,
                ShortName,
                ProgramID,
                CreditHours,
                TheoryHours,
                LabHours,
                CourseType,
                CourseLevel,
                SemesterNo,
                IsMandatory,
                IsActive,
                Description,
                Objectives,
                PrerequisiteCourseID,
                CreatedBy,
                CreatedAt
            )
            VALUES (
                @CourseCode,
                @CourseTitle,
                @ShortName,
                @ProgramID,
                @CreditHours,
                @TheoryHours,
                @LabHours,
                @CourseType,
                @CourseLevel,
                @SemesterNo,
                @IsMandatory,
                @IsActive,
                @Description,
                @Objectives,
                @PrerequisiteCourseID,
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

    public async Task<bool> UpdateAsync(CourseFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Courses SET
                CourseCode = @CourseCode,
                CourseTitle = @CourseTitle,
                ShortName = @ShortName,
                ProgramID = @ProgramID,
                CreditHours = @CreditHours,
                TheoryHours = @TheoryHours,
                LabHours = @LabHours,
                CourseType = @CourseType,
                CourseLevel = @CourseLevel,
                SemesterNo = @SemesterNo,
                IsMandatory = @IsMandatory,
                IsActive = @IsActive,
                Description = @Description,
                Objectives = @Objectives,
                PrerequisiteCourseID = @PrerequisiteCourseID,
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
            UPDATE dbo.Courses SET
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

    private static CourseFormModel Map(SqlDataReader reader) => new()
    {
        Uid = Convert.ToInt32(reader["Uid"]),
        CourseCode = reader["CourseCode"] as string ?? string.Empty,
        CourseTitle = reader["CourseTitle"] as string ?? string.Empty,
        ShortName = reader["ShortName"] as string,
        ProgramId = Convert.ToInt32(reader["ProgramID"]),
        CreditHours = Convert.ToByte(reader["CreditHours"]),
        TheoryHours = Convert.ToByte(reader["TheoryHours"]),
        LabHours = Convert.ToByte(reader["LabHours"]),
        CourseType = reader["CourseType"] as string ?? string.Empty,
        CourseLevel = reader["CourseLevel"] as string ?? string.Empty,
        SemesterNo = reader["SemesterNo"] is DBNull ? null : Convert.ToByte(reader["SemesterNo"]),
        IsMandatory = Convert.ToBoolean(reader["IsMandatory"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        Description = reader["Description"] as string,
        Objectives = reader["Objectives"] as string,
        PrerequisiteCourseId = reader["PrerequisiteCourseID"] is DBNull ? null : Convert.ToInt32(reader["PrerequisiteCourseID"])
    };

    private static void Bind(SqlCommand command, CourseFormModel model)
    {
        command.Parameters.AddWithValue("@CourseCode", model.CourseCode.Trim());
        command.Parameters.AddWithValue("@CourseTitle", model.CourseTitle.Trim());
        command.Parameters.AddWithValue("@ShortName", (object?)model.ShortName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@CreditHours", model.CreditHours);
        command.Parameters.AddWithValue("@TheoryHours", model.TheoryHours);
        command.Parameters.AddWithValue("@LabHours", model.LabHours);
        command.Parameters.AddWithValue("@CourseType", model.CourseType.Trim());
        command.Parameters.AddWithValue("@CourseLevel", model.CourseLevel.Trim());
        command.Parameters.AddWithValue("@SemesterNo", (object?)model.SemesterNo ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsMandatory", model.IsMandatory);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Description", (object?)model.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Objectives", (object?)model.Objectives?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@PrerequisiteCourseID", (object?)model.PrerequisiteCourseId ?? DBNull.Value);
    }
}
