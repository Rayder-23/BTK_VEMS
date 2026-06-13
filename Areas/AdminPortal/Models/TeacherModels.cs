using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherListItemViewModel
{
    public int TeacherId { get; init; }

    public string EmployeeNo { get; init; } = string.Empty;

    public string TeacherName { get; init; } = string.Empty;

    public string? MobileNo { get; init; }

    public string? Email { get; init; }

    public bool IsActive { get; init; }
}

public sealed class TeacherFormModel
{
    public int TeacherId { get; set; }

    [StringLength(50)]
    [Display(Name = "Employee no.")]
    public string? EmployeeNo { get; set; }

    [Required(ErrorMessage = "Teacher name is required.")]
    [StringLength(200)]
    [Display(Name = "Teacher name")]
    public string TeacherName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Mobile no.")]
    public string? MobileNo { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
