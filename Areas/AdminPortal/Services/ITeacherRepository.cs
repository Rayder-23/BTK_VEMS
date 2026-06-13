using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherRepository
{
    Task<IReadOnlyList<TeacherListItemViewModel>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<TeacherFormModel?> GetAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<bool> EmployeeNoExistsAsync(string? employeeNo, int? excludeTeacherId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string? email, int? excludeTeacherId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int teacherId, int? updatedBy, CancellationToken cancellationToken = default);
}
