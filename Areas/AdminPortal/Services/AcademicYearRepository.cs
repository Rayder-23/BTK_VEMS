using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class AcademicYearRepository : IAcademicYearRepository
{
    private readonly string _connectionString;

    public AcademicYearRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<AcademicYearListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT
                AcademicYearID,
                YearName,
                StartDate,
                EndDate,
                IsCurrent,
                IsActive,
                CreatedOn
            FROM dbo.AcademicYears
            WHERE (@Search IS NULL
                   OR YearName LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + """
             ORDER BY StartDate DESC, YearName;
            """;

        var list = new List<AcademicYearListItem>();
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

    public async Task<AcademicYearFormModel?> GetAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                AcademicYearID,
                YearName,
                StartDate,
                EndDate,
                IsCurrent,
                IsActive,
                CreatedOn
            FROM dbo.AcademicYears
            WHERE AcademicYearID = @AcademicYearID;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AcademicYearID", academicYearId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapForm(reader);
    }

    public async Task<bool> NameExistsAsync(
        string yearName,
        int? excludeId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.AcademicYears
            WHERE YearName = @YearName
              AND (@ExcludeId IS NULL OR AcademicYearID <> @ExcludeId);
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@YearName", yearName.Trim());
        command.Parameters.AddWithValue("@ExcludeId", (object?)excludeId ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> InsertAsync(AcademicYearFormModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (model.IsCurrent)
            {
                await ClearCurrentAsync(connection, transaction, null, cancellationToken);
            }

            const string sql = """
                INSERT INTO dbo.AcademicYears (
                    YearName,
                    StartDate,
                    EndDate,
                    IsCurrent,
                    IsActive
                )
                VALUES (
                    @YearName,
                    @StartDate,
                    @EndDate,
                    @IsCurrent,
                    @IsActive
                );
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;

            await using var command = new SqlCommand(sql, connection, transaction);
            Bind(command, model);
            var newId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(AcademicYearFormModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            if (model.IsCurrent)
            {
                await ClearCurrentAsync(connection, transaction, model.AcademicYearId, cancellationToken);
            }

            const string sql = """
                UPDATE dbo.AcademicYears SET
                    YearName = @YearName,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    IsCurrent = @IsCurrent,
                    IsActive = @IsActive
                WHERE AcademicYearID = @AcademicYearID;
                """;

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@AcademicYearID", model.AcademicYearId);
            Bind(command, model);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return rows > 0;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> SetActiveAsync(int academicYearId, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.AcademicYears
            SET IsActive = @IsActive,
                IsCurrent = CASE WHEN @IsActive = 0 THEN 0 ELSE IsCurrent END
            WHERE AcademicYearID = @AcademicYearID;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AcademicYearID", academicYearId);
        command.Parameters.AddWithValue("@IsActive", isActive);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.AcademicYears WHERE AcademicYearID = @AcademicYearID;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@AcademicYearID", academicYearId);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task ClearCurrentAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int? exceptId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.AcademicYears
            SET IsCurrent = 0
            WHERE IsCurrent = 1
              AND (@ExceptId IS NULL OR AcademicYearID <> @ExceptId);
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@ExceptId", (object?)exceptId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Bind(SqlCommand command, AcademicYearFormModel model)
    {
        command.Parameters.AddWithValue("@YearName", model.YearName.Trim());
        command.Parameters.AddWithValue("@StartDate", (object?)model.StartDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", (object?)model.EndDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsCurrent", model.IsCurrent);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }

    private static AcademicYearListItem MapListItem(SqlDataReader reader) => new()
    {
        AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
        YearName = reader["YearName"] as string ?? string.Empty,
        StartDate = reader["StartDate"] is DBNull ? null : Convert.ToDateTime(reader["StartDate"]),
        EndDate = reader["EndDate"] is DBNull ? null : Convert.ToDateTime(reader["EndDate"]),
        IsCurrent = Convert.ToBoolean(reader["IsCurrent"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
    };

    private static AcademicYearFormModel MapForm(SqlDataReader reader) => new()
    {
        AcademicYearId = Convert.ToInt32(reader["AcademicYearID"]),
        YearName = reader["YearName"] as string ?? string.Empty,
        StartDate = reader["StartDate"] is DBNull ? null : Convert.ToDateTime(reader["StartDate"]),
        EndDate = reader["EndDate"] is DBNull ? null : Convert.ToDateTime(reader["EndDate"]),
        IsCurrent = Convert.ToBoolean(reader["IsCurrent"]),
        IsActive = Convert.ToBoolean(reader["IsActive"]),
        CreatedOn = reader["CreatedOn"] is DBNull ? null : Convert.ToDateTime(reader["CreatedOn"])
    };
}
