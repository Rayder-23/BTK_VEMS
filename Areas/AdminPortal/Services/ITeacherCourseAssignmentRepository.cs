using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherCourseAssignmentRepository
{
    Task<TeacherAssignmentSummaryViewModel?> GetTeacherSummaryAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherCourseAssignmentListItem>> ListByTeacherAsync(
        int teacherId,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<TeacherCourseAssignmentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<TeacherCourseAssignmentLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> AssignmentExistsAsync(
        TeacherCourseAssignmentFormModel model,
        int? excludeUid,
        CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherCourseAssignmentFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherCourseAssignmentFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
}
