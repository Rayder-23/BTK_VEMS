using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherAssignmentRepository
{
    Task<IReadOnlyList<TeacherAssignmentListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<TeacherAssignmentFormModel?> GetAsync(int teacherAssignmentId, CancellationToken cancellationToken = default);

    Task<TeacherAssignmentLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherAssignmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherAssignmentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int teacherAssignmentId, CancellationToken cancellationToken = default);
}
