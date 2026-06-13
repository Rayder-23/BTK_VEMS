using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface ICourseSectionRepository
{
    Task<IReadOnlyList<CourseSectionListItem>> ListAsync(string? search, CancellationToken cancellationToken = default);

    Task<CourseSectionFormModel?> GetAsync(int courseSectionId, CancellationToken cancellationToken = default);

    Task<CourseSectionLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        int academicYearId,
        int courseId,
        string? sectionName,
        int? excludeCourseSectionId,
        CancellationToken cancellationToken = default);

    Task<int> InsertAsync(CourseSectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(CourseSectionFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int courseSectionId, CancellationToken cancellationToken = default);
}
