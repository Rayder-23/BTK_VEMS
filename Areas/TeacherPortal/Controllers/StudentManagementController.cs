using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class StudentManagementController : TeacherPortalBaseController
{
    public IActionResult Students() =>
        Placeholder("Students", "Student rosters for your classes will appear here.");

    public IActionResult Attendance() =>
        Placeholder("Attendance", "Record and review student attendance.");

    public IActionResult Performance() =>
        Placeholder("Performance", "Track student academic performance over time.");
}
