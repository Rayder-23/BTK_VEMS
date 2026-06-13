using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherAssignmentListItem
{
    public int TeacherAssignmentId { get; init; }
    public string YearName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string? ClassSectionDisplay { get; init; }
}

public sealed class TeacherAssignmentLookups
{
    public IReadOnlyList<StudentLookupItem> AcademicYears { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ClassSections { get; init; } = [];
}

public sealed class TeacherAssignmentFormPageViewModel
{
    public TeacherAssignmentFormModel Form { get; set; } = new();
    public TeacherAssignmentLookups Lookups { get; set; } = new();
}

public sealed class TeacherAssignmentFormModel
{
    public int TeacherAssignmentId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Teacher is required.")]
    [Display(Name = "Teacher")]
    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Display(Name = "Class section")]
    public int? ClassSectionId { get; set; }
}
