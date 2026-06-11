using VEMS.Areas.TeacherPortal.Models;

namespace VEMS.Areas.TeacherPortal.Services;

public interface IClassRepository
{
    Task<IReadOnlyList<ClassListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<ClassFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<ClassLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ClassCodeExistsAsync(string classCode, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ClassFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default);
}
