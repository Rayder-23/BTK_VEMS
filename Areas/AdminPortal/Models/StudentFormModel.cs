using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentFormModel
{
    public int StudentId { get; set; }

    [StringLength(50)]
    [Display(Name = "Registration no.")]
    public string? RegistrationNo { get; set; }

    [Required(ErrorMessage = "Student name is required.")]
    [StringLength(200)]
    [Display(Name = "Student name")]
    public string StudentName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Mobile no.")]
    public string? MobileNo { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
