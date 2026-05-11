using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class FeeController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Fee";
        ViewData["PageTitle"] = "Fee";
        return View(AdminModuleCatalog.CreateModulePage("Fee"));
    }
}
