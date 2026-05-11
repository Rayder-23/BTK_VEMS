namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentLoginUser
{
    public int Uid { get; init; }

    public string? StudentId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Role { get; init; } = "Student";
}
