using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IAcademicYearRepository
{
    Task<IReadOnlyList<AcademicYearListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<AcademicYearFormModel?> GetAsync(int academicYearId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string yearName, int? excludeId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(AcademicYearFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(AcademicYearFormModel model, CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(int academicYearId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int academicYearId, CancellationToken cancellationToken = default);
}
