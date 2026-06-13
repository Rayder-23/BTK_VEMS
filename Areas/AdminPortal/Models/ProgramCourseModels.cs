using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ProgramCourseListItem
{
    public int ProgramCourseId { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
}

public sealed class ProgramCourseLookups
{
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
}

public sealed class ProgramCourseFormPageViewModel
{
    public ProgramCourseFormModel Form { get; set; } = new();
    public ProgramCourseLookups Lookups { get; set; } = new();
}

public sealed class ProgramCourseFormModel
{
    public int ProgramCourseId { get; set; }

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}
