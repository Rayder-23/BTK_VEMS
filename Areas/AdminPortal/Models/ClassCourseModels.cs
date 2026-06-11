using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ClassCourseListItem
{
    public int Uid { get; init; }
    public string ClassCode { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? TeacherName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ClassCourseLookups
{
    public IReadOnlyList<StudentLookupItem> Classes { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];
}

public sealed class ClassCourseFormPageViewModel
{
    public ClassCourseFormModel Form { get; set; } = new();
    public ClassCourseLookups Lookups { get; set; } = new();
}

public sealed class ClassCourseFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Class is required.")]
    [Display(Name = "Class")]
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Display(Name = "Teacher")]
    public int? TeacherId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime? CreatedAt { get; set; }

    public string? ClassDisplay { get; set; }
    public string? CourseDisplay { get; set; }
}
