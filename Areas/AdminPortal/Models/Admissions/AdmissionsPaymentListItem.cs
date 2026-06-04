namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class AdmissionsPaymentListItem
{
    public int Uid { get; init; }
    public string ApplicationNo { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PaymentStatus { get; init; }
    public decimal? PaymentAmount { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string ApplicationStatus { get; init; } = string.Empty;
}
