using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models.Fee;

public sealed class FeeLookupItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class FeeHeadListItem
{
    public short Uid { get; init; }
    public string HeadCode { get; init; } = string.Empty;
    public string HeadName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
}

public sealed class FeeHeadFormModel
{
    public short Uid { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Head code")]
    public string HeadCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Head name")]
    public string HeadName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = "Academic";

    [Display(Name = "Mandatory")]
    public bool IsMandatory { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? Description { get; set; }
}

public sealed class FeeStructureListItem
{
    public int Uid { get; init; }
    public string StructureName { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string Semester { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public int DetailCount { get; init; }
    public decimal TotalAmount { get; init; }
    public bool IsActive { get; init; }
}

public sealed class FeeStructureFormModel
{
    public int Uid { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Structure name")]
    public string StructureName { get; set; } = string.Empty;

    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Required, StringLength(20)]
    public string Semester { get; set; } = "Fall";

    [Display(Name = "Academic year")]
    public short AcademicYear { get; set; } = (short)DateTime.Today.Year;

    public bool IsActive { get; set; } = true;
}

public sealed class FeeStructureDetailLine
{
    public int Uid { get; init; }
    public int StructureId { get; init; }
    public short FeeHeadId { get; init; }
    public string FeeHeadName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly? DueDate { get; init; }
    public decimal LateFinePerDay { get; init; }
    public decimal MaxLateFine { get; init; }
}

public sealed class FeeStructureDetailFormModel
{
    public int Uid { get; set; }
    public int StructureId { get; set; }

    [Display(Name = "Fee head")]
    [Range(1, short.MaxValue)]
    public short FeeHeadId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }

    [Display(Name = "Late fine / day")]
    public decimal LateFinePerDay { get; set; }

    [Display(Name = "Max late fine")]
    public decimal MaxLateFine { get; set; }
}

public sealed class FeeStructureDetailsPageModel
{
    public FeeStructureListItem Structure { get; init; } = new();
    public IReadOnlyList<FeeStructureDetailLine> Lines { get; init; } = [];
    public FeeStructureDetailFormModel NewLine { get; set; } = new();
    public IReadOnlyList<FeeLookupItem> FeeHeads { get; init; } = [];
}

public sealed class ChallanListItem
{
    public int Uid { get; init; }
    public string ChallanNo { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string RegistrationNo { get; init; } = string.Empty;
    public string Semester { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public DateOnly DueDate { get; init; }
    public decimal NetPayable { get; init; }
    public decimal AmountPaid { get; init; }
    public string Status { get; init; } = string.Empty;
    public string DisplayStatus { get; init; } = string.Empty;
}

public sealed class ChallanGenerateFormModel
{
    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Display(Name = "Fee structure")]
    [Range(1, int.MaxValue)]
    public int StructureId { get; set; }

    [DataType(DataType.Date)]
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(15));

    [StringLength(300)]
    public string? Remarks { get; set; }

    public decimal DiscountAmount { get; set; }
}

public sealed class ChallanDetailLine
{
    public int Uid { get; init; }
    public string FeeHeadName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LateFine { get; init; }
    public decimal NetAmount { get; init; }
}

public sealed class ChallanDetailsPageModel
{
    public ChallanListItem Header { get; init; } = new();
    public IReadOnlyList<ChallanDetailLine> Lines { get; init; } = [];
    public IReadOnlyList<PaymentListItem> Payments { get; init; } = [];
}

public sealed class PaymentListItem
{
    public int Uid { get; init; }
    public int ChallanId { get; init; }
    public string ChallanNo { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public DateOnly PaymentDate { get; init; }
    public string PaymentMode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ReceiptNo { get; init; }
}

public sealed class PaymentFormModel
{
    [Display(Name = "Challan")]
    [Range(1, int.MaxValue)]
    public int ChallanId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal AmountPaid { get; set; }

    [DataType(DataType.Date)]
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(30)]
    [Display(Name = "Payment mode")]
    public string PaymentMode { get; set; } = "Cash";

    [StringLength(100)]
    [Display(Name = "Transaction ref")]
    public string? TransactionRef { get; set; }

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(100)]
    public string? BranchName { get; set; }

    [StringLength(50)]
    [Display(Name = "Cheque no")]
    public string? ChequeNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Cheque date")]
    public DateOnly? ChequeDate { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }

    public string? ChallanNo { get; set; }
    public string? StudentName { get; set; }
    public decimal NetPayable { get; set; }
    public decimal AmountPaidSoFar { get; set; }
    public decimal Balance => Math.Max(0, NetPayable - AmountPaidSoFar);
}

public sealed class PaymentReceiptViewModel
{
    public int PaymentId { get; init; }
    public string ReceiptNo { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public string? IssuedBy { get; init; }
    public string ChallanNo { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string RegistrationNo { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public string PaymentMode { get; init; } = string.Empty;
    public DateOnly PaymentDate { get; init; }
    public string? TransactionRef { get; init; }
    public decimal ChallanNetPayable { get; init; }
    public decimal ChallanTotalPaid { get; init; }
}

public sealed class ConcessionListItem
{
    public int Uid { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string? FeeHeadName { get; init; }
    public string ConcessionType { get; init; } = string.Empty;
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ConcessionFormModel
{
    public int Uid { get; set; }

    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Display(Name = "Fee head (optional — leave empty for all heads)")]
    public short? FeeHeadId { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Concession type")]
    public string ConcessionType { get; set; } = "Merit";

    [Range(0, 100)]
    [Display(Name = "Discount %")]
    public decimal DiscountPercent { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Discount amount")]
    public decimal DiscountAmount { get; set; }

    [StringLength(100)]
    public string? ApprovedBy { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? ApprovalDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly? ValidTo { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class FeeDashboardTile
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
    public required string AccentClass { get; init; }
}
