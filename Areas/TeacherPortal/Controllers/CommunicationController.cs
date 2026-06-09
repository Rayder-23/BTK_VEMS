using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class CommunicationController : TeacherPortalBaseController
{
    public IActionResult Messages() =>
        Placeholder("Messages", "Send and receive messages with students and staff.");

    public IActionResult Announcements() =>
        Placeholder("Announcements", "Post class and school-wide announcements.");

    public IActionResult Meetings() =>
        Placeholder("Meetings", "Schedule and join virtual or in-person meetings.");
}
