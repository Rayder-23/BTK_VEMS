using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherClassCourseListItem
{
    public int Uid { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string ClassCode { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class TeacherClassCourseLookups
{
    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ClassSectionCourses { get; init; } = [];
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class TeacherClassCourseFormPageViewModel
{
    public TeacherClassCourseFormModel Form { get; set; } = new();
    public TeacherClassCourseLookups Lookups { get; set; } = new();
}

public sealed class TeacherClassCourseFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Teacher is required.")]
    [Display(Name = "Teacher")]
    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Class section course is required.")]
    [Display(Name = "Class section / course")]
    [Range(1, int.MaxValue)]
    public int ClassSectionCourseId { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "Role")]
    [StringLength(20)]
    public string Role { get; set; } = "Lead";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime? CreatedAt { get; set; }

    public string? TeacherDisplay { get; set; }
    public string? ClassSectionCourseDisplay { get; set; }
}
