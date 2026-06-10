using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<EmployeeListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<EmployeeFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(EmployeeFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(EmployeeFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);

    Task<bool> EmployeeIdExistsAsync(string employeeId, int? excludeUid = null, CancellationToken cancellationToken = default);

    Task<bool> CnicExistsAsync(string cnic, int? excludeUid = null, CancellationToken cancellationToken = default);
}
