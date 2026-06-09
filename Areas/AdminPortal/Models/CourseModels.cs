using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class CourseListItem
{
    public int Uid { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public byte CreditHours { get; init; }
    public string CourseType { get; init; } = string.Empty;
    public string CourseLevel { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CourseLookups
{
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> PrerequisiteCourses { get; init; } = [];
    public IReadOnlyList<string> CourseTypes { get; init; } = [];
    public IReadOnlyList<string> CourseLevels { get; init; } = [];
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
    [Range(1, 6, ErrorMessage = "Credit hours must be between 1 and 6.")]
    public byte CreditHours { get; set; } = 3;

    [Display(Name = "Theory hours")]
    [Range(0, 255)]
    public byte TheoryHours { get; set; }

    [Display(Name = "Lab hours")]
    [Range(0, 255)]
    public byte LabHours { get; set; }

    [Required(ErrorMessage = "Course type is required.")]
    [StringLength(20)]
    [Display(Name = "Course type")]
    public string CourseType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course level is required.")]
    [StringLength(20)]
    [Display(Name = "Course level")]
    public string CourseLevel { get; set; } = string.Empty;

    [Display(Name = "Semester no.")]
    [Range(1, 12, ErrorMessage = "Semester no. must be between 1 and 12.")]
    public byte? SemesterNo { get; set; }

    [Display(Name = "Mandatory")]
    public bool IsMandatory { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Objectives { get; set; }

    [Display(Name = "Prerequisite course")]
    public int? PrerequisiteCourseId { get; set; }
}
