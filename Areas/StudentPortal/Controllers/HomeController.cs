using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

[Area("StudentPortal")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Student Portal";
        return View();
    }
}
