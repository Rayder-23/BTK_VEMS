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
        ViewData["Title"] = "Examination";
        ViewData["PageTitle"] = "Examination";
        return View(ExaminationDashboardCatalog.Tiles);
    }
}
