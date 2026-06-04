using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class DashboardController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Portal";

        var model = new AdminDashboardViewModel
        {
            Modules = AdminModuleCatalog.Modules
        };

        return View(model);
    }
}
