using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IClassCourseRepository
{
    Task<IReadOnlyList<ClassCourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<ClassCourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<ClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int classId, int courseId, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ClassCourseFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassCourseFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
}
