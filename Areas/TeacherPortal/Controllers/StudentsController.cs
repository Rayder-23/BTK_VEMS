using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class StudentsController : TeacherPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Class Roster";
        ViewData["Description"] = "Student rosters for your classes will appear here.";
        return View("ModulePlaceholder");
    }

    public IActionResult Advising()
    {
        ViewData["Title"] = "Advising";
        ViewData["Description"] = "Student advising notes and meetings will appear here.";
        return View("ModulePlaceholder");
    }
}
