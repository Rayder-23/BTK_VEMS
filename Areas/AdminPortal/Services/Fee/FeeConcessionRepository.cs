using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeeConcessionRepository : IFeeConcessionRepository
{
    private readonly string _connectionString;

    public FeeConcessionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<ConcessionListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT c.Uid, s.FirstName + ' ' + s.LastName AS StudentName,
                   fh.HeadName AS FeeHeadName, c.ConcessionType, c.DiscountPercent, c.DiscountAmount,
                   c.ValidFrom, c.ValidTo, c.IsActive
            FROM dbo.Concessions c
            INNER JOIN dbo.Students s ON c.StudentID = s.Uid
            LEFT JOIN dbo.ref_FeeHeads fh ON c.FeeHeadID = fh.Uid
            ORDER BY c.ValidFrom DESC, c.Uid DESC;
            """;

        var list = new List<ConcessionListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ConcessionListItem
            {
                Uid = FeeSql.ToInt32(reader, "Uid"),
                StudentName = reader["StudentName"] as string ?? "",
                FeeHeadName = reader["FeeHeadName"] as string,
                ConcessionType = reader["ConcessionType"] as string ?? "",
                DiscountPercent = FeeSql.ToDecimal(reader, "DiscountPercent"),
                DiscountAmount = FeeSql.ToDecimal(reader, "DiscountAmount"),
                ValidFrom = FeeSql.ToDateOnly(reader, "ValidFrom"),
                ValidTo = FeeSql.ToNullableDateOnly(reader, "ValidTo"),
                IsActive = FeeSql.ToBoolean(reader, "IsActive")
            });
        }

        return list;
    }

    public async Task<ConcessionFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, StudentID, FeeHeadID, ConcessionType, DiscountPercent, DiscountAmount,
                   ApprovedBy, ApprovalDate, ValidFrom, ValidTo, Remarks, IsActive
            FROM dbo.Concessions WHERE Uid = @Uid;
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

    public async Task<int> InsertAsync(ConcessionFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Concessions
                (StudentID, FeeHeadID, ConcessionType, DiscountPercent, DiscountAmount,
                 ApprovedBy, ApprovalDate, ValidFrom, ValidTo, Remarks, IsActive, CreatedBy, CreatedAt)
            VALUES
                (@StudentId, @FeeHeadId, @ConcessionType, @DiscountPercent, @DiscountAmount,
                 @ApprovedBy, @ApprovalDate, @ValidFrom, @ValidTo, @Remarks, @IsActive, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        Bind(command, model, createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(ConcessionFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Concessions SET
                StudentID = @StudentId, FeeHeadID = @FeeHeadId, ConcessionType = @ConcessionType,
                DiscountPercent = @DiscountPercent, DiscountAmount = @DiscountAmount,
                ApprovedBy = @ApprovedBy, ApprovalDate = @ApprovalDate,
                ValidFrom = @ValidFrom, ValidTo = @ValidTo, Remarks = @Remarks, IsActive = @IsActive,
                UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        Bind(command, model, updatedBy ?? 1);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Concessions SET IsActive = 0, UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<decimal> GetApplicableDiscountForHeadAsync(
        int studentId,
        short feeHeadId,
        decimal amount,
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ConcessionType, DiscountPercent, DiscountAmount
            FROM dbo.Concessions
            WHERE StudentID = @StudentId AND IsActive = 1
              AND ValidFrom <= @AsOf AND (ValidTo IS NULL OR ValidTo >= @AsOf)
              AND (FeeHeadID IS NULL OR FeeHeadID = @FeeHeadId)
            ORDER BY CASE WHEN FeeHeadID IS NULL THEN 1 ELSE 0 END;
            """;

        decimal totalDiscount = 0;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StudentId", studentId);
        command.Parameters.AddWithValue("@FeeHeadId", feeHeadId);
        command.Parameters.AddWithValue("@AsOf", asOf.ToDateTime(TimeOnly.MinValue));
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var pct = FeeSql.ToDecimal(reader, "DiscountPercent");
            var flat = FeeSql.ToDecimal(reader, "DiscountAmount");
            if (pct > 0)
            {
                totalDiscount += Math.Round(amount * pct / 100m, 2);
            }
            else if (flat > 0)
            {
                totalDiscount += flat;
            }
        }

        return Math.Min(totalDiscount, amount);
    }

    private static void Bind(SqlCommand command, ConcessionFormModel model, int createdBy)
    {
        command.Parameters.AddWithValue("@StudentId", model.StudentId);
        command.Parameters.AddWithValue("@FeeHeadId", (object?)model.FeeHeadId ?? DBNull.Value);
        command.Parameters.AddWithValue("@ConcessionType", model.ConcessionType.Trim());
        command.Parameters.AddWithValue("@DiscountPercent", model.DiscountPercent);
        command.Parameters.AddWithValue("@DiscountAmount", model.DiscountAmount);
        command.Parameters.AddWithValue("@ApprovedBy", (object?)model.ApprovedBy?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@ApprovalDate", model.ApprovalDate.HasValue ? model.ApprovalDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@ValidFrom", model.ValidFrom.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@ValidTo", model.ValidTo.HasValue ? model.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@Remarks", (object?)model.Remarks?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
    }

    private static ConcessionFormModel Map(SqlDataReader reader) => new()
    {
        Uid = FeeSql.ToInt32(reader, "Uid"),
        StudentId = FeeSql.ToInt32(reader, "StudentID"),
        FeeHeadId = reader["FeeHeadID"] is DBNull ? null : FeeSql.ToInt16(reader, "FeeHeadID"),
        ConcessionType = reader["ConcessionType"] as string ?? "Merit",
        DiscountPercent = FeeSql.ToDecimal(reader, "DiscountPercent"),
        DiscountAmount = FeeSql.ToDecimal(reader, "DiscountAmount"),
        ApprovedBy = reader["ApprovedBy"] as string,
        ApprovalDate = FeeSql.ToNullableDateOnly(reader, "ApprovalDate"),
        ValidFrom = FeeSql.ToDateOnly(reader, "ValidFrom"),
        ValidTo = FeeSql.ToNullableDateOnly(reader, "ValidTo"),
        Remarks = reader["Remarks"] as string,
        IsActive = FeeSql.ToBoolean(reader, "IsActive")
    };
}
