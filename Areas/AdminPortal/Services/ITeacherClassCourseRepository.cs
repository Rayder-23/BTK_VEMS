using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherClassCourseRepository
{
    Task<IReadOnlyList<TeacherClassCourseListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<TeacherClassCourseFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<TeacherClassCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int teacherId, int classSectionCourseId, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherClassCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherClassCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default);
}
