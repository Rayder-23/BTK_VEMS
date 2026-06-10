using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VEMS.Areas.AdminPortal.Controllers;

[Area("AdminPortal")]
[Authorize(AuthenticationSchemes = AdminPortalAuth.Scheme)]
public abstract class AdminBaseController : Controller
{
    /// <summary>Session key for <c>dbo.EmployeeLogin.Uid</c> when staff signs in with a real login row.</summary>
    public const string StaffLoginUidSessionKey = "AdminEmployeeLoginUid";

    /// <summary>
    /// Returns a valid <c>EmployeeLogin.Uid</c> for FK columns, or null when the admin session
    /// is not linked to <c>dbo.EmployeeLogin</c> (e.g. built-in admin/admin login).
    /// </summary>
    protected int? ResolveStaffLoginUid()
    {
        var claim = User.FindFirst(AdminPortalAuth.EmployeeLoginUidClaim)?.Value;
        return int.TryParse(claim, out var uid) && uid > 0 ? uid : null;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["AdminUsername"] = User.Identity?.Name ?? "Admin";
        base.OnActionExecuting(context);
    }
}
