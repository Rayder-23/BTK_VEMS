using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentCourseRegistrationRepository
{
    Task<IReadOnlyList<StudentCourseRegistrationListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<StudentCourseRegistrationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<StudentCourseRegistrationLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int studentId, int courseSectionId, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentCourseRegistrationFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentCourseRegistrationFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);
}
