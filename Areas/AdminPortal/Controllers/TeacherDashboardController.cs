using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/teachers/dashboard")]
public sealed class TeacherDashboardController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Overview";
        ViewData["PageTitle"] = "Teachers · Overview";
        return View();
    }
}
