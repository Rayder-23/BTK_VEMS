namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentChallanSummary
{
    public int Uid { get; init; }
    public string ChallanNo { get; init; } = string.Empty;
    public string Semester { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal NetPayable { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal Balance => NetPayable - AmountPaid;
    public string Status { get; init; } = string.Empty;
    public string DisplayStatus { get; init; } = string.Empty;
}

public sealed class StudentChallanLine
{
    public string FeeHeadName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LateFine { get; init; }
    public decimal NetAmount { get; init; }
}

public sealed class StudentChallanPageModel
{
    public StudentChallanSummary? Challan { get; init; }
    public IReadOnlyList<StudentChallanLine> Lines { get; init; } = [];
}

public sealed class StudentFeeHistoryPageModel
{
    public IReadOnlyList<StudentChallanSummary> Challans { get; init; } = [];
}
