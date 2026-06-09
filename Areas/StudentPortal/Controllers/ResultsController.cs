using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

public class ResultsController : StudentPortalBaseController
{
    public IActionResult Assignments()
    {
        ViewData["Title"] = "Assignments";
        return View();
    }

    public IActionResult Quizzes()
    {
        ViewData["Title"] = "Quizzes";
        return View();
    }

    public IActionResult Exams()
    {
        ViewData["Title"] = "Exams";
        return View();
    }
}
