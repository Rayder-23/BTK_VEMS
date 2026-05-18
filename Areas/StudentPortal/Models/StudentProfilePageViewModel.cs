namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentProfilePageViewModel
{
    public StudentProfileViewModel Profile { get; init; } = new();

    public StudentChangePasswordViewModel ChangePassword { get; set; } = new();
}
