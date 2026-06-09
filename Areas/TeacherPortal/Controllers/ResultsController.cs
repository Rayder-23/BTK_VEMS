using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class ResultsController : TeacherPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Grades";
        ViewData["Description"] = "Enter and review student grades.";
        return View("ModulePlaceholder");
    }

    public IActionResult Assignments()
    {
        ViewData["Title"] = "Assignments";
        ViewData["Description"] = "Review and grade student assignments.";
        return View("ModulePlaceholder");
    }
}
