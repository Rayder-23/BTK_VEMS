using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherAssignmentSummaryViewModel
{
    public int TeacherId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;
}

public sealed class TeacherCourseAssignmentListItem
{
    public int Uid { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string ClassCode { get; init; } = string.Empty;

    public string CourseTitle { get; init; } = string.Empty;

    public string CourseCode { get; init; } = string.Empty;

    public string Semester { get; init; } = string.Empty;

    public short AcademicYear { get; init; }

    public string? DayOfWeek { get; init; }

    public TimeSpan? StartTime { get; init; }

    public TimeSpan? EndTime { get; init; }

    public string? RoomNo { get; init; }

    public bool IsActive { get; init; }
}

public sealed class TeacherCourseAssignmentLookups
{
    public IReadOnlyList<StudentLookupItem> Classes { get; init; } = [];

    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];

    public IReadOnlyList<string> Semesters { get; init; } = [];

    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];
}

public sealed class TeacherCourseAssignmentFormModel
{
    public int Uid { get; set; }

    public int TeacherId { get; set; }

    [Required(ErrorMessage = "Class is required.")]
    [Display(Name = "Class")]
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Semester is required.")]
    [StringLength(20)]
    public string Semester { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(2000, 2100)]
    public short AcademicYear { get; set; } = (short)DateTime.UtcNow.Year;

    [StringLength(20)]
    [Display(Name = "Day of week")]
    public string? DayOfWeek { get; set; }

    [Display(Name = "Start time")]
    public TimeSpan? StartTime { get; set; }

    [Display(Name = "End time")]
    public TimeSpan? EndTime { get; set; }

    [StringLength(30)]
    [Display(Name = "Room no.")]
    public string? RoomNo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? Remarks { get; set; }
}

public sealed class TeacherCourseAssignmentFormPageViewModel
{
    public TeacherAssignmentSummaryViewModel Teacher { get; set; } = new();

    public TeacherCourseAssignmentFormModel Form { get; set; } = new();

    public TeacherCourseAssignmentLookups Lookups { get; set; } = new();
}

public sealed class TeacherAssignmentsPageViewModel
{
    public TeacherAssignmentSummaryViewModel Teacher { get; set; } = new();

    public IReadOnlyList<TeacherCourseAssignmentListItem> Assignments { get; set; } = [];
}
