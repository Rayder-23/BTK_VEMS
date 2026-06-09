using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class HomeController : TeacherPortalBaseController
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");
}
