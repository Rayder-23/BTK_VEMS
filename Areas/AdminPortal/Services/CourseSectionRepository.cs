using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class CourseSectionRepository : ICourseSectionRepository
{
    private readonly string _connectionString;

    public CourseSectionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<CourseSectionListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                cs.CourseSectionID,
                ay.YearName,
                ISNULL(c.CourseCode + N' · ', N'') + c.CourseName AS CourseName,
                cs.SectionName,
                cs.Capacity
            FROM dbo.CourseSections cs
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Courses c ON cs.CourseID = c.CourseID
            WHERE (@Search IS NULL
                   OR ay.YearName LIKE @Search
                   OR c.CourseName LIKE @Search
                   OR c.CourseCode LIKE @Search
                   OR cs.SectionName LIKE @Search)
            ORDER BY ay.YearName DESC, c.CourseName, cs.SectionName;
            """;

        var list = new List<CourseSectionListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CourseSectionListItem
            {
                CourseSectionId = Convert.ToInt32(reader["CourseSectionID"]),
                YearName = reader["YearName"] as string ?? string.Empty,
                CourseName = reader["CourseName"] as string ?? string.Empty,
                SectionName = reader["SectionName"] as string,
                Capacity = reader["Capacity"] is DBNull ? null : Convert.ToInt32(reader["Capacity"])
            });
        }

        return list;
    }

    public async Task<CourseSectionFormModel?> GetAsync(int courseSectionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CourseSectionID, AcademicYearID, CourseID, SectionName, Capacity
            FROM dbo.CourseSections
            WHERE CourseSectionID = @CourseSectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseSectionId", courseSectionId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapForm(reader) : null;
    }

    public async Task<CourseSectionLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string yearsSql = """
            SELECT AcademicYearID, YearName
            FROM dbo.AcademicYears
            WHERE IsActive = 1
            ORDER BY StartDate DESC, YearName;
            """;

        const string coursesSql = """
            SELECT CourseID, ISNULL(CourseCode + N' · ', N'') + CourseName
            FROM dbo.Courses
            WHERE IsActive = 1
            ORDER BY CourseName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new CourseSectionLookups
        {
            AcademicYears = await ReadLookupAsync(connection, yearsSql, cancellationToken),
            Courses = await ReadLookupAsync(connection, coursesSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int academicYearId,
        int courseId,
        string? sectionName,
        int? excludeCourseSectionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.CourseSections
            WHERE AcademicYearID = @AcademicYearId
              AND CourseID = @CourseId
              AND ISNULL(SectionName, N'') = ISNULL(@SectionName, N'')
              AND (@ExcludeCourseSectionId IS NULL OR CourseSectionID <> @ExcludeCourseSectionId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AcademicYearId", academicYearId);
        command.Parameters.AddWithValue("@CourseId", courseId);
        command.Parameters.AddWithValue("@SectionName", string.IsNullOrWhiteSpace(sectionName) ? DBNull.Value : sectionName.Trim());
        command.Parameters.AddWithValue("@ExcludeCourseSectionId", (object?)excludeCourseSectionId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(CourseSectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.CourseSections (AcademicYearID, CourseID, SectionName, Capacity)
            VALUES (@AcademicYearId, @CourseId, @SectionName, @Capacity);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(CourseSectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.CourseSections SET
                AcademicYearID = @AcademicYearId,
                CourseID = @CourseId,
                SectionName = @SectionName,
                Capacity = @Capacity
            WHERE CourseSectionID = @CourseSectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseSectionId", model.CourseSectionId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int courseSectionId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.CourseSections WHERE CourseSectionID = @CourseSectionId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CourseSectionId", courseSectionId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, CourseSectionFormModel model)
    {
        command.Parameters.AddWithValue("@AcademicYearId", model.AcademicYearId);
        command.Parameters.AddWithValue("@CourseId", model.CourseId);
        command.Parameters.AddWithValue("@SectionName", string.IsNullOrWhiteSpace(model.SectionName) ? DBNull.Value : model.SectionName.Trim());
        command.Parameters.AddWithValue("@Capacity", model.Capacity.HasValue ? model.Capacity.Value : DBNull.Value);
    }

    private static CourseSectionFormModel MapForm(SqlDataReader reader) => new()
    {
        CourseSectionId = Convert.ToInt32(reader["CourseSectionID"]),
        AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
        CourseId = Convert.ToInt32(reader["CourseID"]),
        SectionName = reader["SectionName"] as string,
        Capacity = reader["Capacity"] is DBNull ? null : Convert.ToInt32(reader["Capacity"])
    };

    private static async Task<IReadOnlyList<StudentLookupItem>> ReadLookupAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var list = new List<StudentLookupItem>();
        await using var command = new SqlCommand(sql, connection);
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
}
