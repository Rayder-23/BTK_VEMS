using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VEMS.Areas.AdminPortal.Controllers;

[Area("AdminPortal")]
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
        var uid = HttpContext.Session.GetInt32(StaffLoginUidSessionKey);
        return uid is > 0 ? uid : null;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var adminUsername = HttpContext.Session.GetString(LoginController.AdminSessionKey);
        if (string.IsNullOrWhiteSpace(adminUsername))
        {
            context.Result = RedirectToAction("Index", "Login", new { area = "AdminPortal" });
            return;
        }

        ViewData["AdminUsername"] = adminUsername;
        base.OnActionExecuting(context);
    }
}
