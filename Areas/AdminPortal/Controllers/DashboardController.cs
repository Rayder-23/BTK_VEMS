using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class DashboardController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        ViewData["PageTitle"] = "Dashboard";

        var model = new AdminDashboardViewModel
        {
            Statistics = AdminModuleCatalog.Statistics,
            Modules = AdminModuleCatalog.Modules
        };

        return View(model);
    }
}
