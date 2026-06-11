using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ICourseRepository
{
    Task<IReadOnlyList<CourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        int? programId = null,
        CancellationToken cancellationToken = default);

    Task<CourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<CourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> CourseCodeExistsAsync(string courseCode, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(CourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default);
}
