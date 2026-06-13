using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherCourseLinkRepository
{
    Task<IReadOnlyList<TeacherCourseListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<TeacherCourseFormModel?> GetAsync(int teacherCourseId, CancellationToken cancellationToken = default);

    Task<TeacherCourseLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int teacherId, int courseId, int? excludeTeacherCourseId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherCourseFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int teacherCourseId, CancellationToken cancellationToken = default);
}
