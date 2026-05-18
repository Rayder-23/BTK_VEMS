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
        ViewData["Title"] = "Fee Management";
        ViewData["PageTitle"] = "Fee Management";
        return View(FeeDashboardCatalog.Tiles);
    }
}
