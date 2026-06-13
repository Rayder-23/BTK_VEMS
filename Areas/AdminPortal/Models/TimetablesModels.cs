using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TimetableListItem
{
    public int TimetableId { get; init; }
    public string DayName { get; init; } = string.Empty;
    public string PeriodDisplay { get; init; } = string.Empty;
    public string? ClassSectionDisplay { get; init; }
    public string? CourseSectionDisplay { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string? RoomNo { get; init; }
}

public sealed class TimetablesLookups
{
    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Periods { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ClassSections { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> CourseSections { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];
}

public sealed class TimetableFormPageViewModel
{
    public TimetableFormModel Form { get; set; } = new();
    public TimetablesLookups Lookups { get; set; } = new();
}

public sealed class TimetableFormModel
{
    public int TimetableId { get; set; }

    [Required(ErrorMessage = "Day is required.")]
    [StringLength(20)]
    [Display(Name = "Day")]
    public string DayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Period is required.")]
    [Display(Name = "Period")]
    [Range(1, int.MaxValue)]
    public int PeriodId { get; set; }

    [Display(Name = "Class section")]
    public int? ClassSectionId { get; set; }

    [Display(Name = "Course section")]
    public int? CourseSectionId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Teacher is required.")]
    [Display(Name = "Teacher")]
    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [StringLength(50)]
    [Display(Name = "Room no.")]
    public string? RoomNo { get; set; }
}
