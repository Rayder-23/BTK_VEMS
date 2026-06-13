namespace VEMS.Areas.AdminPortal.Models;

public sealed class TimetableSlotListItem
{
    public int AssignmentUid { get; init; }

    public int ClassId { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string ClassCode { get; init; } = string.Empty;

    public int CourseId { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public int TeacherId { get; init; }

    public string TeacherName { get; init; } = string.Empty;

    public string EmployeeCode { get; init; } = string.Empty;

    public string Semester { get; init; } = string.Empty;

    public short AcademicYear { get; init; }

    public string? DayOfWeek { get; init; }

    public TimeSpan? StartTime { get; init; }

    public TimeSpan? EndTime { get; init; }

    public string? RoomNo { get; init; }

    public bool IsActive { get; init; }
}

public sealed class TimetableLookups
{
    public IReadOnlyList<StudentLookupItem> Classes { get; init; } = [];

    public IReadOnlyList<StudentLookupItem> Teachers { get; init; } = [];

    public IReadOnlyList<string> Semesters { get; init; } = [];

    public IReadOnlyList<short> AcademicYears { get; init; } = [];

    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];
}

public sealed class TimetableIndexViewModel
{
    public int? ClassId { get; init; }

    public int? TeacherId { get; init; }

    public string Semester { get; init; } = string.Empty;

    public short AcademicYear { get; init; }

    public string ViewMode { get; init; } = "class";

    public TimetableLookups Lookups { get; init; } = new();

    public IReadOnlyList<TimetableSlotListItem> ScheduledSlots { get; init; } = [];

    public IReadOnlyList<TimetableSlotListItem> UnscheduledSlots { get; init; } = [];

    public IReadOnlyDictionary<string, IReadOnlyList<TimetableSlotListItem>> SlotsByDay { get; init; }
        = new Dictionary<string, IReadOnlyList<TimetableSlotListItem>>();
}
