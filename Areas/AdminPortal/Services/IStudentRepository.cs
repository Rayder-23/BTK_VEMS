using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentRepository
{
    Task<IReadOnlyList<StudentListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<StudentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<StudentLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);
}
