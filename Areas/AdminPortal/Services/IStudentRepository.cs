using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentRepository
{
    Task<IReadOnlyList<StudentListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<StudentFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);
}
