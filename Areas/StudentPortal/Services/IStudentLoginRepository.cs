namespace VEMS.Areas.StudentPortal.Services;

public interface IStudentLoginRepository
{
    Task<StudentLoginUser?> ValidateCredentialsAsync(string username, string password);
}
