using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.TeacherPortal.Models;

namespace VEMS.Areas.TeacherPortal.Services;

public interface ITeacherAcademicRepository
{
    Task<int?> ResolveTeacherIdAsync(
        string? employeeCode,
        int? employeeUid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassListItem>> ListAssignedClassesAsync(
        int teacherId,
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourseListItem>> ListAssignedCoursesAsync(
        int teacherId,
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
