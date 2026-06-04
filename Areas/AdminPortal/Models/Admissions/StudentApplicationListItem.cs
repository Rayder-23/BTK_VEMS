namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class StudentApplicationListItem
{
    public int Uid { get; init; }
    public string ApplicationNo { get; init; } = string.Empty;
    public DateTime ApplicationDate { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string ApplicationStatus { get; init; } = string.Empty;
    public string SourceChannel { get; init; } = string.Empty;
    public string MobileNo { get; init; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
