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
        ViewData["Title"] = "HR Management";
        ViewData["PageTitle"] = "HR Management";
        return View(HrModuleCatalog.ModuleNavItems);
    }
}
