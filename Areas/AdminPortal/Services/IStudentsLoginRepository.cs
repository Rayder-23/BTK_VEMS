using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public interface IStudentsLoginRepository
{
    Task<IReadOnlyList<StudentLoginListItemViewModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<StudentLoginFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentLookupItem>> GetStudentsWithoutLoginAsync(CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, int? excludeUid = null, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentLoginFormModel model, string plainPassword, int? createdBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentLoginFormModel model, string? plainPassword, int? updatedBy, CancellationToken cancellationToken = default);
}
