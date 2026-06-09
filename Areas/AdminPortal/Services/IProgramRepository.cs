using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IProgramRepository
{
    Task<IReadOnlyList<ProgramListItem>> ListAsync(string? search, bool activeOnly, CancellationToken cancellationToken = default);

    Task<ProgramListItem?> GetListItemAsync(int uid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentLookupItem>> GetProgramOptionsAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<ProgramFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<ProgramLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ProgramCodeExistsAsync(string programCode, int? excludeUid, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ProgramFormModel model, int? createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProgramFormModel model, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(int uid, CancellationToken cancellationToken = default);
}
