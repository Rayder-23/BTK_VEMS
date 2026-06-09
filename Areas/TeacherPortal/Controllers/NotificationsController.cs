using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class NotificationsController : TeacherPortalBaseController
{
    public IActionResult Inbox() =>
        Placeholder("Inbox", "Read notifications and system messages.");

    public IActionResult Alerts() =>
        Placeholder("Alerts", "Review priority alerts and reminders.");
}
