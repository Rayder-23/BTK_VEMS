using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ClassCourseListItem
{
    public int ClassSectionCourseId { get; init; }
    public string YearName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string SectionName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
}

public sealed class ClassCourseLookups
{
    public IReadOnlyList<StudentLookupItem> ClassSections { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
}

public sealed class ClassCourseFormPageViewModel
{
    public ClassCourseFormModel Form { get; set; } = new();
    public ClassCourseLookups Lookups { get; set; } = new();
}

public sealed class ClassCourseFormModel
{
    public int ClassSectionCourseId { get; set; }

    [Required(ErrorMessage = "Class section is required.")]
    [Display(Name = "Class section")]
    [Range(1, int.MaxValue)]
    public int ClassSectionId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}
