namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class AdmissionsStatusCount
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class AdmissionsSummaryViewModel
{
    public int TotalApplications { get; init; }
    public IReadOnlyList<AdmissionsStatusCount> ByApplicationStatus { get; init; } = Array.Empty<AdmissionsStatusCount>();
    public IReadOnlyList<AdmissionsStatusCount> ByPaymentStatus { get; init; } = Array.Empty<AdmissionsStatusCount>();
    public IReadOnlyList<AdmissionsStatusCount> BySourceChannel { get; init; } = Array.Empty<AdmissionsStatusCount>();
}
