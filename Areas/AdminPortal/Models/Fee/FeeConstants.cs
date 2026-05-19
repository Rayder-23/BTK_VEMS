namespace VEMS.Areas.AdminPortal.Models.Fee;

public static class FeeConstants
{
    public static readonly string[] ChallanStatuses = ["Unpaid", "Partial", "Paid", "Overdue", "Cancelled"];

    public static readonly string[] PaymentModes =
    [
        "Cash", "Bank Transfer", "Online", "Cheque", "JazzCash", "EasyPaisa"
    ];

    /// <summary>Matches <c>CK_Payments_Status</c> on dbo.Payments.</summary>
    public static readonly string[] PaymentStatuses = ["Pending", "Verified", "Rejected"];

    public const string PaymentStatusVerified = "Verified";

    public static readonly string[] FeeHeadCategories =
    [
        "Tuition", "Admission", "Exam", "Transport", "Hostel", "Miscellaneous", "Other"
    ];

    public static readonly string[] ConcessionTypes = ["Percentage", "FixedAmount"];
}
