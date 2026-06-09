namespace VEMS.Areas.TeacherPortal.Services;

public interface ITeacherLoginRepository
{
    Task<TeacherLoginValidationResult> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
