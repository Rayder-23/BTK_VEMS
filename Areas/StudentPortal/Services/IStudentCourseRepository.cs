using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public interface IStudentCourseRepository
{
    Task<StudentAllCoursesPageModel> GetAssignedCoursesAsync(
        int studentUid,
        CancellationToken cancellationToken = default);
}
