namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentListItemViewModel
{
    public int Uid { get; init; }
    public string RegistrationNo { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string FatherName { get; init; } = string.Empty;
    public string? ProgramName { get; init; }
    public string? RollNo { get; init; }
    public DateTime AdmissionDate { get; init; }
    public bool IsActive { get; init; }
}
