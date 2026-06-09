using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

public class CoursesController : StudentPortalBaseController
{
    public IActionResult AllCourses()
    {
        ViewData["Title"] = "All Courses";
        return View();
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
