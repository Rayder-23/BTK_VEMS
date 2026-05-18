using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentLoginFormModel
{
    public int Uid { get; set; }

    [Display(Name = "Student")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a student.")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Active";

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters.")]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Display(Name = "Must change password on next login")]
    public bool MustChangePassword { get; set; } = true;

    public string? StudentDisplayName { get; set; }

    public string? RegistrationNo { get; set; }
}
