namespace VEMS.Areas.TeacherPortal.Services;

public interface ITeacherAccountRepository
{
    Task<string?> GetPasswordHashByLoginUidAsync(int loginUid, CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAsync(int loginUid, string passwordHash, CancellationToken cancellationToken = default);
}
