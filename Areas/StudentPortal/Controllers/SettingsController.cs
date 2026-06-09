using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

public class SettingsController : StudentPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Settings";
        return View();
    }
}
