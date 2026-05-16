using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Employee ID is required.")]
    [StringLength(250)]
    [Display(Name = "Employee ID")]
    public string EmployeeID { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Employee name")]
    public string? EmployeeName { get; set; }

    [StringLength(250)]
    public string? CNIC { get; set; }

    [StringLength(250)]
    [Display(Name = "Father name")]
    public string? FatherName { get; set; }

    [StringLength(250)]
    [Display(Name = "Date of birth (text)")]
    public string? DOB { get; set; }

    [StringLength(250)]
    [Display(Name = "Mobile")]
    public string? MobileNo { get; set; }

    [StringLength(250)]
    public string? Department { get; set; }

    [StringLength(250)]
    public string? Designation { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of joining")]
    public DateTime? DateOfJoining { get; set; }

    [StringLength(250)]
    [Display(Name = "Employee status")]
    public string? EmployeeStatus { get; set; }

    [Display(Name = "Modified by")]
    public string? ModifiedBy { get; set; }

    [Display(Name = "Modified on (text)")]
    public string? ModifiedOn { get; set; }

    public string? Details { get; set; }

    [StringLength(50)]
    public string? Project { get; set; }

    [Display(Name = "Carry forward leaves")]
    public double? CarryForwardLeaves { get; set; }

    [Display(Name = "Year 2022")]
    public double? Year2022 { get; set; }

    [Display(Name = "Year 2023")]
    public double? Year2023 { get; set; }

    [Display(Name = "Adjusted / adjusted")]
    public int? AdjustedAjusted { get; set; }

    [Display(Name = "Year 2024")]
    public int? Year2024 { get; set; }

    [Display(Name = "Carry forward leaves 1")]
    public double? CarryForwardLeaves1 { get; set; }

    [Display(Name = "Year 2023 (decimal)")]
    public decimal? Year2023New { get; set; }

    [Display(Name = "Basic salary")]
    public decimal? BasicSalary { get; set; }

    [StringLength(10)]
    [Display(Name = "Apply tax")]
    public string? ApplyTax { get; set; }

    [StringLength(50)]
    [Display(Name = "Gen status")]
    public string? GenStatus { get; set; }
}
