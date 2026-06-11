using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class CourseListItem
{
    public int Uid { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public byte CreditHours { get; init; }
    public byte? SemesterNo { get; init; }
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CourseLookups
{
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
}

public sealed class CourseFormPageViewModel
{
    public CourseFormModel Form { get; set; } = new();
    public CourseLookups Lookups { get; set; } = new();
}

public sealed class CourseFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(20)]
    [Display(Name = "Course code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course title is required.")]
    [StringLength(150)]
    [Display(Name = "Course title")]
    public string CourseTitle { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Short name")]
    public string? ShortName { get; set; }

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Credit hours is required.")]
    [Display(Name = "Credit hours")]
    [Range(1, 255)]
    public byte CreditHours { get; set; } = 3;

    [Display(Name = "Semester no.")]
    [Range(1, 255)]
    public byte? SemesterNo { get; set; }

    [Display(Name = "Mandatory")]
    public bool IsMandatory { get; set; } = true;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime? CreatedAt { get; set; }
}
