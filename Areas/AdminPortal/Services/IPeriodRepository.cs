using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IPeriodRepository
{
    Task<IReadOnlyList<PeriodListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<PeriodFormModel?> GetAsync(int periodId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(PeriodFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(PeriodFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int periodId, CancellationToken cancellationToken = default);
}
