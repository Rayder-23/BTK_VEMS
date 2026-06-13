using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ICourseRepository
{
    Task<IReadOnlyList<CourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<CourseFormModel?> GetAsync(int courseId, CancellationToken cancellationToken = default);

    Task<bool> CourseCodeExistsAsync(string courseCode, int? excludeCourseId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(CourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int courseId, CancellationToken cancellationToken = default);
}
