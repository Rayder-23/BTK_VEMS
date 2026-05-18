namespace VEMS.Areas.AdminPortal.Models.Fee;

public static class FeeConstants
{
    public static readonly string[] ChallanStatuses = ["Unpaid", "Partial", "Paid", "Overdue", "Cancelled"];

    public static readonly string[] PaymentModes =
    [
        "Cash", "Bank Transfer", "Online", "Cheque", "JazzCash", "EasyPaisa"
    ];

    public static readonly string[] FeeHeadCategories =
    [
        "Tuition", "Admission", "Exam", "Transport", "Hostel", "Miscellaneous", "Other"
    ];

    public static readonly string[] ConcessionTypes = ["Percentage", "FixedAmount"];
}
