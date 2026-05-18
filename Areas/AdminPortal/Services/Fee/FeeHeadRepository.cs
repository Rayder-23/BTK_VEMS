using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeeHeadRepository : IFeeHeadRepository
{
    private readonly string _connectionString;

    public FeeHeadRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<FeeHeadListItem>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT Uid, HeadCode, HeadName, Category, IsMandatory, IsActive
            FROM dbo.ref_FeeHeads
            WHERE (@Search IS NULL OR HeadName LIKE @Search OR Category LIKE @Search OR HeadCode LIKE @Search)
            """ + (activeOnly ? " AND IsActive = 1" : "") + " ORDER BY HeadName;";

        var list = new List<FeeHeadListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim()}%");
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeHeadListItem
            {
                Uid = (short)FeeSql.ToInt32(reader, "Uid"),
                HeadCode = reader["HeadCode"] as string ?? "",
                HeadName = reader["HeadName"] as string ?? "",
                Category = reader["Category"] as string ?? "",
                IsMandatory = FeeSql.ToBoolean(reader, "IsMandatory"),
                IsActive = FeeSql.ToBoolean(reader, "IsActive")
            });
        }

        return list;
    }

    public async Task<FeeHeadFormModel?> GetAsync(short uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, HeadCode, HeadName, Category, IsMandatory, IsActive, Description
            FROM dbo.ref_FeeHeads WHERE Uid = @Uid;
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

    public async Task<bool> HeadCodeExistsAsync(string headCode, short? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM dbo.ref_FeeHeads
            WHERE HeadCode = @HeadCode AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@HeadCode", headCode.Trim());
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
    }

    public async Task<short> InsertAsync(FeeHeadFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ref_FeeHeads (HeadCode, HeadName, Category, IsMandatory, IsActive, Description, CreatedBy, CreatedAt)
            VALUES (@HeadCode, @HeadName, @Category, @IsMandatory, @IsActive, @Description, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model, createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt16(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(FeeHeadFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ref_FeeHeads SET
                HeadCode = @HeadCode, HeadName = @HeadName, Category = @Category,
                IsMandatory = @IsMandatory, IsActive = @IsActive, Description = @Description,
                UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model, updatedBy ?? 0);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(short uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.ref_FeeHeads SET IsActive = 0, UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void Bind(SqlCommand command, FeeHeadFormModel model, int createdBy)
    {
        command.Parameters.AddWithValue("@HeadCode", model.HeadCode.Trim());
        command.Parameters.AddWithValue("@HeadName", model.HeadName.Trim());
        command.Parameters.AddWithValue("@Category", model.Category.Trim());
        command.Parameters.AddWithValue("@IsMandatory", model.IsMandatory);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@Description", (object?)model.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
    }

    private static FeeHeadFormModel Map(SqlDataReader reader) => new()
    {
        Uid = (short)FeeSql.ToInt32(reader, "Uid"),
        HeadCode = reader["HeadCode"] as string ?? "",
        HeadName = reader["HeadName"] as string ?? "",
        Category = reader["Category"] as string ?? "",
        IsMandatory = FeeSql.ToBoolean(reader, "IsMandatory"),
        IsActive = FeeSql.ToBoolean(reader, "IsActive"),
        Description = reader["Description"] as string
    };
}
