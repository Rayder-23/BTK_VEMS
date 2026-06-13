using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentCourseEnrollmentRepository
{
    Task<IReadOnlyList<StudentCourseEnrollmentListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<StudentCourseEnrollmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<StudentCourseEnrollmentLookups> GetLookupsAsync(
        int? studentId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int studentId, int classSectionCourseId, int? excludeUid, CancellationToken cancellationToken = default);

    Task<bool> EnrollmentBelongsToStudentAsync(int enrollmentId, int studentId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentCourseEnrollmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentCourseEnrollmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default);
}
