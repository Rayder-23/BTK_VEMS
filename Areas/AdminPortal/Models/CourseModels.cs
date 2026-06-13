using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class CourseListItem
{
    public int CourseId { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public int? CreditHours { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CourseFormModel
{
    public int CourseId { get; set; }

    [StringLength(20)]
    [Display(Name = "Course code")]
    public string? CourseCode { get; set; }

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(200)]
    [Display(Name = "Course name")]
    public string CourseName { get; set; } = string.Empty;

    [Display(Name = "Credit hours")]
    [Range(0, int.MaxValue)]
    public int? CreditHours { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
