using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class AttendanceController : TeacherPortalBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Mark Attendance";
        ViewData["Description"] = "Record daily attendance for your classes.";
        return View("ModulePlaceholder");
    }

    public IActionResult Records()
    {
        ViewData["Title"] = "Attendance Records";
        ViewData["Description"] = "Historical attendance records will appear here.";
        return View("ModulePlaceholder");
    }
}
