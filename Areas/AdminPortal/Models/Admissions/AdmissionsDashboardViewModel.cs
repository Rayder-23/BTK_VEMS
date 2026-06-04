namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class AdmissionsDashboardViewModel
{
    public int TotalApplications { get; init; }
    public int PendingApplications { get; init; }
    public int ApprovedApplications { get; init; }
    public int PaidApplications { get; init; }
    public int OnlineApplications { get; init; }
    public IReadOnlyList<StudentApplicationListItem> RecentApplications { get; init; } = Array.Empty<StudentApplicationListItem>();
}
