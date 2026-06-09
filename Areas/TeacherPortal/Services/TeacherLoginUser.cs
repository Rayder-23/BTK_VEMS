namespace VEMS.Areas.TeacherPortal.Services;

public sealed class TeacherLoginUser
{
    public int LoginUid { get; init; }

    public int EmployeeUid { get; init; }

    public string Username { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string EmployeeCode { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
