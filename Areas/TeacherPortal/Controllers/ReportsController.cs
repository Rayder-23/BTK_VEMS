using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class ReportsController : TeacherPortalBaseController
{
    public IActionResult AttendanceReports() =>
        Placeholder("Attendance Reports", "Generate attendance summaries and export reports.");

    public IActionResult MarksReports() =>
        Placeholder("Marks Reports", "View and export student marks and grade reports.");

    public IActionResult Analytics() =>
        Placeholder("Analytics", "Explore learning analytics and performance trends.");
}
