using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IProgramEnrollmentRepository
{
    Task<IReadOnlyList<ProgramEnrollmentListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<ProgramEnrollmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<ProgramEnrollmentLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        int studentId,
        int programId,
        int academicYearId,
        int? excludeUid,
        CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ProgramEnrollmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProgramEnrollmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);
}
