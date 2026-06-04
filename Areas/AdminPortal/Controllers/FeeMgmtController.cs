using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee")]
public sealed class FeeMgmtController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "Fee Management · Dashboard";
        return View(FeeDashboardCatalog.Tiles);
    }
}
