namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class AdmissionFeePickResult
{
    public decimal Amount { get; init; }
    public bool Found { get; init; }
    public string Message { get; init; } = string.Empty;
}
