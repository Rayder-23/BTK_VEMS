using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IClassCourseRepository
{
    Task<IReadOnlyList<ClassCourseListItem>> ListAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<ClassCourseFormModel?> GetAsync(int classSectionCourseId, CancellationToken cancellationToken = default);

    Task<ClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int classSectionId, int courseId, int? excludeClassSectionCourseId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int classSectionCourseId, CancellationToken cancellationToken = default);
}
