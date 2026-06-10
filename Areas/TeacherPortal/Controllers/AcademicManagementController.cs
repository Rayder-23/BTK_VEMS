using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class AcademicManagementController : TeacherPortalBaseController
{
    private readonly ITeacherAcademicRepository _academic;
    private readonly ITeacherAccountRepository _accounts;

    public AcademicManagementController(
        ITeacherAcademicRepository academic,
        ITeacherAccountRepository accounts)
    {
        _academic = academic;
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<IActionResult> Classes(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "My Classes";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var teacherId = await ResolveTeacherIdAsync(_academic, _accounts, cancellationToken);
        if (teacherId is null)
        {
            ViewData["NoTeacherProfile"] = true;
            return View(Array.Empty<Models.ClassListItem>());
        }

        var items = await _academic.ListAssignedClassesAsync(
            teacherId.Value,
            search,
            activeOnly: !showInactive,
            cancellationToken);

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Courses(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "My Courses";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var teacherId = await ResolveTeacherIdAsync(_academic, _accounts, cancellationToken);
        if (teacherId is null)
        {
            ViewData["NoTeacherProfile"] = true;
            return View(Array.Empty<AdminPortal.Models.CourseListItem>());
        }

        var items = await _academic.ListAssignedCoursesAsync(
            teacherId.Value,
            search,
            activeOnly: !showInactive,
            cancellationToken);

        return View(items);
    }

    public IActionResult Timetable() =>
        Placeholder("Timetable", "Your weekly teaching schedule will appear here.");

    public IActionResult LessonPlans() =>
        Placeholder("Lesson Plans", "Create and organize lesson plans for your classes.");
}
