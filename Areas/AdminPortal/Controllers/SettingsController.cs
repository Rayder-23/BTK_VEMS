using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings")]
public sealed class SettingsController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "Settings · Dashboard";
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create() =>
        RedirectToAction("Create", "Configurations", new { area = "AdminPortal" });
}
