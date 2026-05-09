using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.ManagementPortal.Controllers;

[Area("ManagementPortal")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Management Portal";
        return View();
    }
}
