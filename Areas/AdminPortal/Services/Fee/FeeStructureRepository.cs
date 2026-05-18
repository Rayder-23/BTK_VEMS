using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Fee;

namespace VEMS.Areas.AdminPortal.Services.Fee;

public sealed class FeeStructureRepository : IFeeStructureRepository
{
    private readonly string _connectionString;

    public FeeStructureRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<FeeStructureListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT fs.Uid, fs.StructureName, p.ProgramName, fs.Semester, fs.AcademicYear, fs.IsActive,
                   COUNT(fsd.Uid) AS DetailCount,
                   ISNULL(SUM(fsd.Amount), 0) AS TotalAmount
            FROM dbo.FeeStructures fs
            INNER JOIN dbo.ref_Programs p ON fs.ProgramID = p.Uid
            LEFT JOIN dbo.FeeStructureDetails fsd ON fsd.StructureID = fs.Uid
            GROUP BY fs.Uid, fs.StructureName, p.ProgramName, fs.Semester, fs.AcademicYear, fs.IsActive
            ORDER BY fs.AcademicYear DESC, fs.StructureName;
            """;

        var list = new List<FeeStructureListItem>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeStructureListItem
            {
                Uid = FeeSql.ToInt32(reader, "Uid"),
                StructureName = reader["StructureName"] as string ?? "",
                ProgramName = reader["ProgramName"] as string ?? "",
                Semester = reader["Semester"] as string ?? "",
                AcademicYear = FeeSql.ToInt16(reader, "AcademicYear"),
                IsActive = FeeSql.ToBoolean(reader, "IsActive"),
                DetailCount = FeeSql.ToInt32(reader, "DetailCount"),
                TotalAmount = FeeSql.ToDecimal(reader, "TotalAmount")
            });
        }

        return list;
    }

    public async Task<FeeStructureFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Uid, StructureName, ProgramID, Semester, AcademicYear, IsActive
            FROM dbo.FeeStructures WHERE Uid = @Uid;
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

        return new FeeStructureFormModel
        {
            Uid = FeeSql.ToInt32(reader, "Uid"),
            StructureName = reader["StructureName"] as string ?? "",
            ProgramId = FeeSql.ToInt16(reader, "ProgramID"),
            Semester = reader["Semester"] as string ?? "",
            AcademicYear = FeeSql.ToInt16(reader, "AcademicYear"),
            IsActive = FeeSql.ToBoolean(reader, "IsActive")
        };
    }

    public async Task<bool> ExistsAsync(short programId, string semester, short academicYear, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM dbo.FeeStructures
            WHERE ProgramID = @ProgramId AND Semester = @Semester AND AcademicYear = @AcademicYear
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ProgramId", programId);
        command.Parameters.AddWithValue("@Semester", semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", academicYear);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
    }

    public async Task<int> InsertAsync(FeeStructureFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.FeeStructures (StructureName, ProgramID, Semester, AcademicYear, IsActive, CreatedBy, CreatedAt)
            VALUES (@StructureName, @ProgramId, @Semester, @AcademicYear, @IsActive, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        BindStructure(command, model);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(FeeStructureFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.FeeStructures SET
                StructureName = @StructureName, ProgramID = @ProgramId, Semester = @Semester,
                AcademicYear = @AcademicYear, IsActive = @IsActive,
                UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        BindStructure(command, model);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.FeeStructures SET IsActive = 0, UpdatedBy = @UpdatedBy, UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<FeeStructureDetailsPageModel?> GetDetailsPageAsync(int structureId, CancellationToken cancellationToken = default)
    {
        var structures = await ListAsync(cancellationToken);
        var structure = structures.FirstOrDefault(s => s.Uid == structureId);
        if (structure is null)
        {
            return null;
        }

        var lines = await GetDetailsForStructureAsync(structureId, cancellationToken);
        return new FeeStructureDetailsPageModel
        {
            Structure = structure,
            Lines = lines,
            NewLine = new FeeStructureDetailFormModel { StructureId = structureId }
        };
    }

    public async Task<bool> DetailExistsAsync(int structureId, short feeHeadId, int? excludeUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) FROM dbo.FeeStructureDetails
            WHERE StructureID = @StructureId AND FeeHeadID = @FeeHeadId
              AND (@ExcludeUid IS NULL OR Uid <> @ExcludeUid);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StructureId", structureId);
        command.Parameters.AddWithValue("@FeeHeadId", feeHeadId);
        command.Parameters.AddWithValue("@ExcludeUid", (object?)excludeUid ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
    }

    public async Task<int> AddDetailAsync(FeeStructureDetailFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.FeeStructureDetails
                (StructureID, FeeHeadID, Amount, DueDate, LateFinePerDay, MaxLateFine, CreatedBy, CreatedAt)
            VALUES
                (@StructureId, @FeeHeadId, @Amount, @DueDate, @LateFinePerDay, @MaxLateFine, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StructureId", model.StructureId);
        command.Parameters.AddWithValue("@FeeHeadId", model.FeeHeadId);
        command.Parameters.AddWithValue("@Amount", model.Amount);
        command.Parameters.AddWithValue("@DueDate", model.DueDate.HasValue ? model.DueDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@LateFinePerDay", model.LateFinePerDay);
        command.Parameters.AddWithValue("@MaxLateFine", model.MaxLateFine);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        await connection.OpenAsync(cancellationToken);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> DeleteDetailAsync(int detailUid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.FeeStructureDetails WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", detailUid);
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<FeeStructureDetailLine>> GetDetailsForStructureAsync(int structureId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT fsd.Uid, fsd.StructureID, fsd.FeeHeadID, fh.HeadName, fsd.Amount, fsd.DueDate,
                   fsd.LateFinePerDay, fsd.MaxLateFine
            FROM dbo.FeeStructureDetails fsd
            INNER JOIN dbo.ref_FeeHeads fh ON fsd.FeeHeadID = fh.Uid
            WHERE fsd.StructureID = @StructureId
            ORDER BY fh.HeadName;
            """;

        var list = new List<FeeStructureDetailLine>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StructureId", structureId);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new FeeStructureDetailLine
            {
                Uid = FeeSql.ToInt32(reader, "Uid"),
                StructureId = FeeSql.ToInt32(reader, "StructureID"),
                FeeHeadId = FeeSql.ToInt16(reader, "FeeHeadID"),
                FeeHeadName = reader["HeadName"] as string ?? "",
                Amount = FeeSql.ToDecimal(reader, "Amount"),
                DueDate = FeeSql.ToNullableDateOnly(reader, "DueDate"),
                LateFinePerDay = FeeSql.ToDecimal(reader, "LateFinePerDay"),
                MaxLateFine = FeeSql.ToDecimal(reader, "MaxLateFine")
            });
        }

        return list;
    }

    private static void BindStructure(SqlCommand command, FeeStructureFormModel model)
    {
        command.Parameters.AddWithValue("@StructureName", model.StructureName.Trim());
        command.Parameters.AddWithValue("@ProgramId", model.ProgramId);
        command.Parameters.AddWithValue("@Semester", model.Semester.Trim());
        command.Parameters.AddWithValue("@AcademicYear", model.AcademicYear);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
    }
}
