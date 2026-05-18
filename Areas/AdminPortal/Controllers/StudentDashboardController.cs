using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/dashboard")]
public sealed class StudentDashboardController : StudentMgmtBaseController
{
    protected override string ModuleKey => "Dashboard";

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "Student Management · Dashboard";
        return View();
    }

    [HttpGet("reports")]
    public IActionResult Reports()
    {
        ViewData["Title"] = "Reports";
        ViewData["PageTitle"] = "Student Management · Reports";
        return View("Reports");
    }
}
