using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IProgramEnrollmentRepository
{
    Task<IReadOnlyList<ProgramEnrollmentListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<ProgramEnrollmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<ProgramEnrollmentLookups> GetLookupsAsync(
        int? programId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForPeriodAsync(
        int studentId,
        int programId,
        short academicYear,
        byte gradeOrSemester,
        int? excludeUid,
        CancellationToken cancellationToken = default);

    Task<bool> RollNoExistsAsync(
        int programId,
        short academicYear,
        byte gradeOrSemester,
        string rollNo,
        int? excludeUid,
        CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ProgramEnrollmentFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProgramEnrollmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> WithdrawAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
}
