using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/examination")]
public sealed class ExaminationMgmtController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "Examination · Dashboard";
        return View(ExaminationDashboardCatalog.Tiles);
    }
}
