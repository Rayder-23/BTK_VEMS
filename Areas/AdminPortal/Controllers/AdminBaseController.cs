using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VEMS.Areas.AdminPortal.Controllers;

[Area("AdminPortal")]
public abstract class AdminBaseController : Controller
{
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
