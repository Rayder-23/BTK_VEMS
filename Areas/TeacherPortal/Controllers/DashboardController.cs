using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class DashboardController : TeacherPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Teacher Dashboard";
        return View();
    }
}
