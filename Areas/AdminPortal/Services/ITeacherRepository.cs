using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITeacherRepository
{
    Task<IReadOnlyList<TeacherListItemViewModel>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<TeacherFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<TeacherLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? excludeUid, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string? email, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TeacherFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TeacherFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
}
