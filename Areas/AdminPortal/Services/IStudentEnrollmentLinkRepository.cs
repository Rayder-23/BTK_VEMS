using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentEnrollmentLinkRepository
{
    Task<IReadOnlyList<StudentEnrollmentLinkListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<StudentEnrollmentLinkFormModel?> GetAsync(int studentEnrollmentId, CancellationToken cancellationToken = default);

    Task<StudentEnrollmentLinkLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentEnrollmentLinkFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentEnrollmentLinkFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int studentEnrollmentId, CancellationToken cancellationToken = default);
}
