using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.TeacherPortal.Services;

public static class EmployeePasswordVerifier
{
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        return StudentPasswordHasher.VerifyPassword(password, storedHash);
    }
}
