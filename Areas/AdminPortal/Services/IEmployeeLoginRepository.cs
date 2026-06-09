using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IEmployeeLoginRepository
{
    Task<IReadOnlyList<EmployeeLoginListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<EmployeeLoginFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<EmployeeLoginLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentLookupItem>> GetEmployeesWithoutLoginAsync(CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, int? excludeUid = null, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(EmployeeLoginFormModel model, string plainPassword, int? createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(EmployeeLoginFormModel model, string? plainPassword, CancellationToken cancellationToken = default);
}
