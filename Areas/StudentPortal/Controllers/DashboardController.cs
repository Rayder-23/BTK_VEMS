using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

[Area("StudentPortal")]
[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Student Dashboard";
        return View();
    }
}
