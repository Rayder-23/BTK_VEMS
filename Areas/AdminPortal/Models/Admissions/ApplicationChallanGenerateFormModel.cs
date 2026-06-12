using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class ApplicationChallanGenerateFormModel
{
    public int ApplicationUid { get; set; }

    [Display(Name = "Student ID")]
    public string ApplicationNo { get; set; } = string.Empty;

    public string ApplicantName { get; set; } = string.Empty;

    public string ProgramName { get; set; } = string.Empty;

    public short DesiredYear { get; set; }

    [Display(Name = "Fee structure")]
    public int StructureId { get; set; }

    public string StructureLabel { get; set; } = string.Empty;

    [Display(Name = "Admission fee (from structure)")]
    public decimal AdmissionFeeAmount { get; set; }

    public bool FeeStructureFound { get; set; }

    public string? FeeMessage { get; set; }

    [DataType(DataType.Date)]
    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(15));

    [StringLength(300)]
    public string? Remarks { get; set; }

    [Display(Name = "Extra discount")]
    public decimal DiscountAmount { get; set; }
}
