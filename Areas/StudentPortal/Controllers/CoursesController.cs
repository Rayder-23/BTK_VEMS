using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

public class CoursesController : StudentPortalBaseController
{
    private readonly IStudentProfileRepository _profiles;
    private readonly IStudentCourseRepository _courses;

    public CoursesController(IStudentProfileRepository profiles, IStudentCourseRepository courses)
    {
        _profiles = profiles;
        _courses = courses;
    }

    public async Task<IActionResult> AllCourses(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "All Courses";

        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        var model = await _courses.GetAssignedCoursesAsync(studentUid.Value, cancellationToken);
        return View(model);
    }

    public IActionResult Classes()
    {
        ViewData["Title"] = "Classes";
        return View();
    }

    public IActionResult Timetable()
    {
        ViewData["Title"] = "Timetable";
        return View();
    }
}
