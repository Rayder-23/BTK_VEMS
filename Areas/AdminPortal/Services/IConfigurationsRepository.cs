using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IConfigurationsRepository
{
    Task<IReadOnlyList<ConfigurationListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<ConfigurationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ConfigurationFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ConfigurationFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);
}
