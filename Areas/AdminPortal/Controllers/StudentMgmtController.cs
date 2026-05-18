using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students")]
public sealed class StudentMgmtController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Student Management";
        ViewData["PageTitle"] = "Student Management";
        return View(StudentMgmtModuleCatalog.GridModules);
    }
}
