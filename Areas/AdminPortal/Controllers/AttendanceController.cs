using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class AttendanceController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Attendance";
        ViewData["PageTitle"] = "Attendance";
        return View(AdminModuleCatalog.CreateModulePage("Attendance"));
    }
}
