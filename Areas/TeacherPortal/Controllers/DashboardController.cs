using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Teacher Dashboard";
        return View();
    }
}
