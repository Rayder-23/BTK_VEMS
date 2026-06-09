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

    Task<int> InsertAsync(ClassFormModel model, int createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, int? updatedBy, CancellationToken cancellationToken = default);
}
