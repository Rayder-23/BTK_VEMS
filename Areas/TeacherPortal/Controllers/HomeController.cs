using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Teacher Portal";
        return View();
    }
}
