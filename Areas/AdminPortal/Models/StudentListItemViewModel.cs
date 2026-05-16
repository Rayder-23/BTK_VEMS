namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentListItemViewModel
{
    public int Uid { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string GuardianName { get; init; } = string.Empty;
    public string GradeLevel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime EnrolledDate { get; init; }
}
