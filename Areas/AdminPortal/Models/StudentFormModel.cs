using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Guardian name is required.")]
    [StringLength(100)]
    [Display(Name = "Guardian name")]
    public string GuardianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guardian phone is required.")]
    [StringLength(20)]
    [Display(Name = "Guardian phone")]
    public string GuardianPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Grade level is required.")]
    [StringLength(20)]
    [Display(Name = "Grade level")]
    public string GradeLevel { get; set; } = string.Empty;

    [StringLength(100)]
    public string? City { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enrolled date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Enrolled date")]
    public DateTime? EnrolledDate { get; set; }
}
