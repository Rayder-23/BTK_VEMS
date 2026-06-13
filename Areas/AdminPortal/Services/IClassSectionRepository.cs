using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IClassSectionRepository
{
    Task<IReadOnlyList<ClassSectionListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<ClassSectionFormModel?> GetAsync(int classSectionId, CancellationToken cancellationToken = default);

    Task<ClassSectionLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int academicYearId, int classId, int sectionId, int? excludeClassSectionId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ClassSectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassSectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int classSectionId, CancellationToken cancellationToken = default);
}
