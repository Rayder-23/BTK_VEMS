using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ITimetablesRepository
{
    Task<IReadOnlyList<TimetableListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<TimetableFormModel?> GetAsync(int timetableId, CancellationToken cancellationToken = default);

    Task<TimetablesLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TimetableFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TimetableFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int timetableId, CancellationToken cancellationToken = default);
}
