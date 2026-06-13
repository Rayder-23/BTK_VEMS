namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentListItemViewModel
{
    public int StudentId { get; init; }

    public string RegistrationNo { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public string? MobileNo { get; init; }

    public string? Email { get; init; }

    public DateTime CreatedOn { get; init; }

    public bool IsActive { get; init; }
}
