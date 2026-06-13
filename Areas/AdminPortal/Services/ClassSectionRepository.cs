using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class ClassSectionRepository : IClassSectionRepository
{
    private readonly string _connectionString;

    public ClassSectionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ClassSectionListItem>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                cs.ClassSectionID,
                ay.YearName,
                c.ClassName,
                s.SectionName
            FROM dbo.ClassSections cs
            INNER JOIN dbo.AcademicYears ay ON cs.AcademicYearID = ay.AcademicYearID
            INNER JOIN dbo.Classes c ON cs.ClassID = c.ClassID
            INNER JOIN dbo.Sections s ON cs.SectionID = s.SectionID
            WHERE (@Search IS NULL
                   OR ay.YearName LIKE @Search
                   OR c.ClassName LIKE @Search
                   OR c.ClassCode LIKE @Search
                   OR s.SectionName LIKE @Search)
            ORDER BY ay.YearName DESC, c.ClassName, s.SectionName;
            """;

        var list = new List<ClassSectionListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ClassSectionListItem
            {
                ClassSectionId = Convert.ToInt32(reader["ClassSectionID"]),
                YearName = reader["YearName"] as string ?? string.Empty,
                ClassName = reader["ClassName"] as string ?? string.Empty,
                SectionName = reader["SectionName"] as string ?? string.Empty
            });
        }

        return list;
    }

    public async Task<ClassSectionFormModel?> GetAsync(int classSectionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                ClassSectionID,
                AcademicYearID,
                ClassID,
                SectionID
            FROM dbo.ClassSections
            WHERE ClassSectionID = @ClassSectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionId", classSectionId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapForm(reader);
    }

    public async Task<ClassSectionLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        const string yearsSql = """
            SELECT AcademicYearID, YearName
            FROM dbo.AcademicYears
            WHERE IsActive = 1
            ORDER BY StartDate DESC, YearName;
            """;

        const string classesSql = """
            SELECT ClassID, ClassCode + ' · ' + ClassName
            FROM dbo.Classes
            WHERE IsActive = 1
            ORDER BY SortOrder, ClassName;
            """;

        const string sectionsSql = """
            SELECT SectionID, SectionName
            FROM dbo.Sections
            WHERE IsActive = 1
            ORDER BY SectionName;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new ClassSectionLookups
        {
            AcademicYears = await ReadLookupAsync(connection, yearsSql, cancellationToken),
            Classes = await ReadLookupAsync(connection, classesSql, cancellationToken),
            Sections = await ReadLookupAsync(connection, sectionsSql, cancellationToken)
        };
    }

    public async Task<bool> ExistsAsync(
        int academicYearId,
        int classId,
        int sectionId,
        int? excludeClassSectionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.ClassSections
            WHERE AcademicYearID = @AcademicYearId
              AND ClassID = @ClassId
              AND SectionID = @SectionId
              AND (@ExcludeClassSectionId IS NULL OR ClassSectionID <> @ExcludeClassSectionId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AcademicYearId", academicYearId);
        command.Parameters.AddWithValue("@ClassId", classId);
        command.Parameters.AddWithValue("@SectionId", sectionId);
        command.Parameters.AddWithValue("@ExcludeClassSectionId", (object?)excludeClassSectionId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(ClassSectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ClassSections (AcademicYearID, ClassID, SectionID)
            VALUES (@AcademicYearId, @ClassId, @SectionId);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ClassSectionFormModel model, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ClassSections SET
                AcademicYearID = @AcademicYearId,
                ClassID = @ClassId,
                SectionID = @SectionId
            WHERE ClassSectionID = @ClassSectionId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionId", model.ClassSectionId);
        Bind(command, model);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int classSectionId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.ClassSections WHERE ClassSectionID = @ClassSectionId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClassSectionId", classSectionId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, ClassSectionFormModel model)
    {
        command.Parameters.AddWithValue("@AcademicYearId", model.AcademicYearId);
        command.Parameters.AddWithValue("@ClassId", model.ClassId);
        command.Parameters.AddWithValue("@SectionId", model.SectionId);
    }

    private static ClassSectionFormModel MapForm(SqlDataReader reader) => new()
    {
        ClassSectionId = Convert.ToInt32(reader["ClassSectionID"]),
        AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
        ClassId = Convert.ToInt32(reader["ClassID"]),
        SectionId = Convert.ToInt32(reader["SectionID"])
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
