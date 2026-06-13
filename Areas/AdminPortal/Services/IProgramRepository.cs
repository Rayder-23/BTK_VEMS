using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IProgramRepository
{
    Task<IReadOnlyList<ProgramListItem>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);

    Task<ProgramListItem?> GetListItemAsync(int programId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentLookupItem>> GetProgramOptionsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<ProgramFormModel?> GetAsync(int programId, CancellationToken cancellationToken = default);

    Task<bool> ProgramCodeExistsAsync(string programCode, int? excludeProgramId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ProgramFormModel model, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProgramFormModel model, CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(int programId, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int programId, CancellationToken cancellationToken = default);
}
