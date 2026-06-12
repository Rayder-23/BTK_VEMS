using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentRepository : IStudentRepository
{
    private readonly string _connectionString;

    public StudentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");
    }

    public async Task<IReadOnlyList<StudentListItemViewModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.Uid,
                s.RegistrationNo,
                s.FirstName + ' ' + ISNULL(s.MiddleName + ' ', '') + s.LastName AS DisplayName,
                s.FatherName,
                p.ProgramName,
                s.RollNo,
                s.AdmissionDate,
                s.IsActive
            FROM dbo.Students s
            LEFT JOIN dbo.ref_Programs p ON s.ProgramID = p.Uid
            ORDER BY s.Uid DESC;
            """;

        var list = new List<StudentListItemViewModel>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new StudentListItemViewModel
            {
                Uid = ToInt32(reader, "Uid"),
                RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
                DisplayName = reader["DisplayName"] as string ?? string.Empty,
                FatherName = reader["FatherName"] as string ?? string.Empty,
                ProgramName = reader["ProgramName"] as string,
                RollNo = reader["RollNo"] as string,
                AdmissionDate = reader.GetDateTime(reader.GetOrdinal("AdmissionDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }

        return list;
    }

    public async Task<StudentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Uid,
                RegistrationNo,
                ProgramID,
                AdmissionYear,
                AdmissionDate,
                FirstName,
                MiddleName,
                LastName,
                FatherName,
                DateOfBirth,
                Gender,
                AddressLine1,
                AddressLine2,
                CountryID,
                ProvinceID,
                CityID,
                PostalCode,
                RollNo,
                NIC_No,
                BFORM_No,
                Nationality,
                IsActive,
                StatusRemark
            FROM dbo.Students
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

        return MapRow(reader);
    }

    public async Task<StudentLookups> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var programs = await ReadLookupAsync(
            connection,
            "SELECT Uid, ProgramCode + ' - ' + ProgramName FROM dbo.ref_Programs WHERE IsActive = 1 ORDER BY ProgramName",
            cancellationToken);
        var countries = await ReadLookupAsync(
            connection,
            "SELECT Uid, CountryName FROM dbo.ref_Countries WHERE IsActive = 1 ORDER BY CountryName",
            cancellationToken);
        var provinces = await ReadLookupAsync(
            connection,
            "SELECT Uid, ProvinceName FROM dbo.ref_Provinces WHERE IsActive = 1 ORDER BY ProvinceName",
            cancellationToken);
        var cities = await ReadLookupAsync(
            connection,
            "SELECT Uid, CityName FROM dbo.ref_Cities WHERE IsActive = 1 ORDER BY CityName",
            cancellationToken);

        return new StudentLookups
        {
            Programs = programs,
            Countries = countries,
            Provinces = provinces,
            Cities = cities
        };
    }

    public async Task<int> InsertAsync(StudentFormModel model, int createdBy, CancellationToken cancellationToken = default)
    {
        ValidateRequiredDates(model);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var studentId = await InsertStudentAsync(connection, transaction, model, createdBy, cancellationToken);
            await InsertInitialEnrollmentAsync(connection, transaction, studentId, model, createdBy, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return studentId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(StudentFormModel model, int? updatedBy, CancellationToken cancellationToken = default)
    {
        ValidateRequiredDates(model);

        const string sql = """
            UPDATE dbo.Students
            SET
                RegistrationNo = @RegistrationNo,
                ProgramID = @ProgramID,
                AdmissionYear = @AdmissionYear,
                AdmissionDate = @AdmissionDate,
                FirstName = @FirstName,
                MiddleName = @MiddleName,
                LastName = @LastName,
                FatherName = @FatherName,
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                AddressLine1 = @AddressLine1,
                AddressLine2 = @AddressLine2,
                CountryID = @CountryID,
                ProvinceID = @ProvinceID,
                CityID = @CityID,
                PostalCode = @PostalCode,
                RollNo = @RollNo,
                NIC_No = @NIC_No,
                BFORM_No = @BFORM_No,
                Nationality = @Nationality,
                IsActive = @IsActive,
                StatusRemark = @StatusRemark,
                UpdatedBy = @UpdatedBy,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Uid = @Uid;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", model.Uid);
        AddWriteParameters(command, model);
        command.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dbo.Students WHERE Uid = @Uid;";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Uid", uid);
        await connection.OpenAsync(cancellationToken);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

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
                Id = ToInt32(reader, 0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    private static StudentFormModel MapRow(SqlDataReader reader)
    {
        return new StudentFormModel
        {
            Uid = ToInt32(reader, "Uid"),
            RegistrationNo = reader["RegistrationNo"] as string ?? string.Empty,
            ProgramId = ToInt32(reader, "ProgramID"),
            AdmissionYear = (short)ToInt32(reader, "AdmissionYear"),
            AdmissionDate = reader.GetDateTime(reader.GetOrdinal("AdmissionDate")),
            FirstName = reader["FirstName"] as string ?? string.Empty,
            MiddleName = reader["MiddleName"] as string,
            LastName = reader["LastName"] as string ?? string.Empty,
            FatherName = reader["FatherName"] as string ?? string.Empty,
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            Gender = (reader["Gender"] as string ?? "M").Trim(),
            AddressLine1 = reader["AddressLine1"] as string ?? string.Empty,
            AddressLine2 = reader["AddressLine2"] as string,
            CountryId = ToInt32(reader, "CountryID"),
            ProvinceId = ToInt32(reader, "ProvinceID"),
            CityId = ToInt32(reader, "CityID"),
            PostalCode = reader["PostalCode"] as string,
            RollNo = reader["RollNo"] as string,
            NIC_No = reader["NIC_No"] as string,
            BFORM_No = reader["BFORM_No"] as string,
            Nationality = reader["Nationality"] as string,
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            StatusRemark = reader["StatusRemark"] as string
        };
    }

    private static void AddWriteParameters(SqlCommand command, StudentFormModel model)
    {
        command.Parameters.AddWithValue("@RegistrationNo", model.RegistrationNo.Trim());
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@AdmissionYear", model.AdmissionYear);
        command.Parameters.AddWithValue("@AdmissionDate", model.AdmissionDate!.Value.Date);
        command.Parameters.AddWithValue("@FirstName", model.FirstName.Trim());
        command.Parameters.AddWithValue("@MiddleName", (object?)model.MiddleName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@LastName", model.LastName.Trim());
        command.Parameters.AddWithValue("@FatherName", model.FatherName.Trim());
        command.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth!.Value.Date);
        command.Parameters.AddWithValue("@Gender", model.Gender.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@AddressLine1", model.AddressLine1.Trim());
        command.Parameters.AddWithValue("@AddressLine2", (object?)model.AddressLine2?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@CountryID", model.CountryId);
        command.Parameters.AddWithValue("@ProvinceID", model.ProvinceId);
        command.Parameters.AddWithValue("@CityID", model.CityId);
        command.Parameters.AddWithValue("@PostalCode", (object?)model.PostalCode?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@RollNo", (object?)model.RollNo?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@NIC_No", (object?)model.NIC_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@BFORM_No", (object?)model.BFORM_No?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Nationality", (object?)model.Nationality?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", model.IsActive);
        command.Parameters.AddWithValue("@StatusRemark", (object?)model.StatusRemark?.Trim() ?? DBNull.Value);
    }

    private static int ToInt32(SqlDataReader reader, string column) =>
        ToInt32(reader, reader.GetOrdinal(column));

    private static int ToInt32(SqlDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal));

    private static async Task<int> InsertStudentAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        StudentFormModel model,
        int createdBy,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Students (
                RegistrationNo,
                ProgramID,
                AdmissionYear,
                AdmissionDate,
                FirstName,
                MiddleName,
                LastName,
                FatherName,
                DateOfBirth,
                Gender,
                AddressLine1,
                AddressLine2,
                CountryID,
                ProvinceID,
                CityID,
                PostalCode,
                RollNo,
                NIC_No,
                BFORM_No,
                Nationality,
                IsActive,
                StatusRemark,
                CreatedBy
            )
            OUTPUT INSERTED.Uid
            VALUES (
                @RegistrationNo,
                @ProgramID,
                @AdmissionYear,
                @AdmissionDate,
                @FirstName,
                @MiddleName,
                @LastName,
                @FatherName,
                @DateOfBirth,
                @Gender,
                @AddressLine1,
                @AddressLine2,
                @CountryID,
                @ProvinceID,
                @CityID,
                @PostalCode,
                @RollNo,
                @NIC_No,
                @BFORM_No,
                @Nationality,
                @IsActive,
                @StatusRemark,
                @CreatedBy
            );
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        AddWriteParameters(command, model);
        command.Parameters.AddWithValue("@CreatedBy", createdBy);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task InsertInitialEnrollmentAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int studentId,
        StudentFormModel model,
        int createdBy,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.StudentEnrollments (
                StudentID,
                ProgramID,
                ClassID,
                RollNo,
                AcademicYear,
                GradeOrSemester,
                EnrollmentDate,
                EnrollmentStatus,
                IsActive
            )
            VALUES (
                @StudentID,
                @ProgramID,
                (SELECT TOP 1 Uid FROM dbo.Classes WHERE ProgramID = @ProgramID AND IsActive = 1 ORDER BY Uid),
                @RollNo,
                @AcademicYear,
                @GradeOrSemester,
                @EnrollmentDate,
                @EnrollmentStatus,
                1
            );
            """;

        var rollNo = string.IsNullOrWhiteSpace(model.RollNo)
            ? model.RegistrationNo.Trim()
            : model.RollNo.Trim();

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@StudentID", studentId);
        command.Parameters.AddWithValue("@ProgramID", model.ProgramId);
        command.Parameters.AddWithValue("@AcademicYear", model.AdmissionYear);
        command.Parameters.AddWithValue("@GradeOrSemester", (byte)1);
        command.Parameters.AddWithValue("@RollNo", rollNo);
        command.Parameters.AddWithValue("@EnrollmentDate", model.AdmissionDate!.Value.Date);
        command.Parameters.AddWithValue("@EnrollmentStatus", "Active");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateRequiredDates(StudentFormModel model)
    {
        if (!model.AdmissionDate.HasValue)
        {
            throw new ArgumentException("AdmissionDate is required.", nameof(model));
        }

        if (!model.DateOfBirth.HasValue || model.DateOfBirth.Value.Year < 1900)
        {
            throw new ArgumentException("DateOfBirth is required.", nameof(model));
        }

        if (model.DateOfBirth.Value.Date >= DateTime.Today)
        {
            throw new ArgumentException("DateOfBirth must be before today.", nameof(model));
        }
    }
}
