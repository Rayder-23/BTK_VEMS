using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class SettingsController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Settings";
        ViewData["PageTitle"] = "Settings";
        return View(AdminModuleCatalog.CreateModulePage("Settings"));
    }
}
