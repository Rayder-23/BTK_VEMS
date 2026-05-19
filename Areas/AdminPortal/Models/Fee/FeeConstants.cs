namespace VEMS.Areas.AdminPortal.Models.Fee;

public static class FeeConstants
{
    public static readonly string[] ChallanStatuses = ["Unpaid", "Partial", "Paid", "Overdue", "Cancelled"];

    /// <summary>Matches <c>CK_FeeStructures_Semester</c> / <c>CK_Challans_Semester</c>.</summary>
    public static readonly string[] Semesters = ["Fall", "Spring", "Summer"];

    /// <summary>Matches <c>CK_Payments_Mode</c> on dbo.Payments.</summary>
    public static readonly string[] PaymentModes =
    [
        "Cash", "Bank Transfer", "Online", "Cheque", "JazzCash", "EasyPaisa", "Other"
    ];

    /// <summary>Matches <c>CK_Payments_Status</c> on dbo.Payments.</summary>
    public static readonly string[] PaymentStatuses = ["Pending", "Verified", "Rejected"];

    public const string PaymentStatusVerified = "Verified";

    /// <summary>Matches <c>CK_ref_FeeHeads_Category</c> on dbo.ref_FeeHeads.</summary>
    public static readonly string[] FeeHeadCategories =
    [
        "Academic", "Administrative", "Facility", "Exam", "Hostel", "Other"
    ];

    /// <summary>Matches <c>CK_Concessions_Type</c> on dbo.Concessions (classification only; discount math uses <see cref="ConcessionFormModel.DiscountPercent"/> / <see cref="ConcessionFormModel.DiscountAmount"/>).</summary>
    public static readonly string[] ConcessionTypes =
    [
        "Merit", "Need-Based", "Sports", "HEC", "Staff Ward", "Sibling", "Other"
    ];
}
