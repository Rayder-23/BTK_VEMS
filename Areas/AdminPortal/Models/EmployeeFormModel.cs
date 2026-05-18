using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Employee ID is required.")]
    [StringLength(20)]
    [Display(Name = "Employee ID")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "CNIC is required.")]
    [StringLength(15)]
    public string CNIC { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Father name")]
    public string? FatherName { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DOB { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(150)]
    public string? Specialization { get; set; }

    [StringLength(150)]
    public string? Qualification { get; set; }

    [StringLength(50)]
    [Display(Name = "Employee type")]
    public string? EmployeeType { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "Joined date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Joined date")]
    public DateTime? JoinedDate { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
