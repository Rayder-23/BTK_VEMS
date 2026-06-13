using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ISectionRepository
{
    Task<IReadOnlyList<SectionListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<SectionFormModel?> GetAsync(int sectionId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string sectionName, int? excludeSectionId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(SectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(SectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(int sectionId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int sectionId, CancellationToken cancellationToken = default);
}
