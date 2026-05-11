using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class HRController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "HR";
        ViewData["PageTitle"] = "HR";
        return View(AdminModuleCatalog.CreateModulePage("HR"));
    }
}
