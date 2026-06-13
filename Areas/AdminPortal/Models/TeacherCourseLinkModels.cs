using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherCourseListItem
{
    public int TeacherCourseId { get; init; }
    public string TeacherName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
}

public sealed class TeacherCourseLookups
{
    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
}

public sealed class TeacherCourseFormPageViewModel
{
    public TeacherCourseFormModel Form { get; set; } = new();
    public TeacherCourseLookups Lookups { get; set; } = new();
}

public sealed class TeacherCourseFormModel
{
    public int TeacherCourseId { get; set; }

    [Required(ErrorMessage = "Teacher is required.")]
    [Display(Name = "Teacher")]
    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}
