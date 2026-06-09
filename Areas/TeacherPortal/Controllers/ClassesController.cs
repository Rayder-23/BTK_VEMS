using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class ClassesController : TeacherPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "My Classes";
        ViewData["Description"] = "View and manage your assigned teaching sections.";
        return View("ModulePlaceholder");
    }

    public IActionResult Timetable()
    {
        ViewData["Title"] = "Timetable";
        ViewData["Description"] = "Your weekly teaching schedule will appear here.";
        return View("ModulePlaceholder");
    }
}
