using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr")]
public sealed class HRController : HrBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "HR Management · Dashboard";
        return View(HrModuleCatalog.ModuleNavItems);
    }
}
