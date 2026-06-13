using VEMS.Areas.TeacherPortal.Models;

namespace VEMS.Areas.TeacherPortal.Services;

public interface IClassRepository
{
    Task<IReadOnlyList<ClassListItem>> ListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<ClassFormModel?> GetAsync(int classId, CancellationToken cancellationToken = default);

    Task<bool> ClassCodeExistsAsync(string classCode, int? excludeClassId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ClassFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClassFormModel model, CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(int classId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int classId, CancellationToken cancellationToken = default);
}
