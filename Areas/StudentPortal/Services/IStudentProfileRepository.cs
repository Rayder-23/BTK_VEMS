using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public interface IStudentProfileRepository
{
    Task<StudentProfileViewModel?> GetByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default);

    Task<int?> ResolveStudentUidByLoginUidAsync(int loginUid, CancellationToken cancellationToken = default);

    Task<string?> GetPasswordHashByStudentUidAsync(int studentUid, CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAsync(int studentUid, string passwordHash, CancellationToken cancellationToken = default);
}
