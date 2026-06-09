using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
[Authorize(AuthenticationSchemes = TeacherPortalAuth.Scheme)]
public abstract class TeacherPortalBaseController : Controller
{
    protected IActionResult Placeholder(string title, string description)
    {
        ViewData["Title"] = title;
        ViewData["Description"] = description;
        return View("ModulePlaceholder");
    }

    protected int? ResolveLoginUid()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var loginUid) ? loginUid : null;
    }
}
