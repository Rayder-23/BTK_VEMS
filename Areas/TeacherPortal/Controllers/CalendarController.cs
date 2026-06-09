using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class CalendarController : TeacherPortalBaseController
{
    public IActionResult AcademicCalendar() =>
        Placeholder("Academic Calendar", "View the institutional academic calendar and term dates.");

    public IActionResult Events() =>
        Placeholder("Events", "Browse and manage upcoming school events.");
}
