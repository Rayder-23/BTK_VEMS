using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/timetable")]
public sealed class TimetableController : AdminBaseController
{
    private readonly ITimetableRepository _timetable;

    public TimetableController(ITimetableRepository timetable)
    {
        _timetable = timetable;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        int? classId,
        int? teacherId,
        string? semester,
        short? academicYear,
        string? viewMode,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Timetable";
        ViewData["PageTitle"] = "Timetable";

        var lookups = await _timetable.GetLookupsAsync(cancellationToken);
        var resolvedSemester = string.IsNullOrWhiteSpace(semester)
            ? lookups.Semesters.FirstOrDefault() ?? "Fall"
            : semester.Trim();
        var resolvedYear = academicYear ?? lookups.AcademicYears.FirstOrDefault();
        var resolvedViewMode = string.Equals(viewMode, "teacher", StringComparison.OrdinalIgnoreCase)
            ? "teacher"
            : "class";

        int? resolvedClassId = classId;
        int? resolvedTeacherId = teacherId;
        if (resolvedViewMode == "class" && resolvedClassId is null or <= 0)
        {
            resolvedClassId = lookups.Classes.FirstOrDefault()?.Id;
        }

        if (resolvedViewMode == "teacher" && resolvedTeacherId is null or <= 0)
        {
            resolvedTeacherId = lookups.Teachers.FirstOrDefault()?.Id;
        }

        var slots = await _timetable.ListAsync(
            resolvedViewMode == "class" ? resolvedClassId : null,
            resolvedViewMode == "teacher" ? resolvedTeacherId : null,
            resolvedSemester,
            resolvedYear,
            activeOnly: true,
            cancellationToken);

        var scheduled = slots.Where(slot => !string.IsNullOrWhiteSpace(slot.DayOfWeek)).ToList();
        var unscheduled = slots.Where(slot => string.IsNullOrWhiteSpace(slot.DayOfWeek)).ToList();
        var slotsByDay = BuildSlotsByDay(scheduled, lookups.DaysOfWeek);

        var model = new TimetableIndexViewModel
        {
            ClassId = resolvedClassId,
            TeacherId = resolvedTeacherId,
            Semester = resolvedSemester,
            AcademicYear = resolvedYear,
            ViewMode = resolvedViewMode,
            Lookups = lookups,
            ScheduledSlots = scheduled,
            UnscheduledSlots = unscheduled,
            SlotsByDay = slotsByDay
        };

        return View(model);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<TimetableSlotListItem>> BuildSlotsByDay(
        IReadOnlyList<TimetableSlotListItem> scheduled,
        IReadOnlyList<string> daysOfWeek)
    {
        var map = new Dictionary<string, IReadOnlyList<TimetableSlotListItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var day in daysOfWeek)
        {
            map[day] = scheduled
                .Where(slot => string.Equals(slot.DayOfWeek, day, StringComparison.OrdinalIgnoreCase))
                .OrderBy(slot => slot.StartTime)
                .ThenBy(slot => slot.ClassName)
                .ToList();
        }

        return map;
    }
}
