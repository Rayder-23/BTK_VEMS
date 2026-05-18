using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public abstract class FeeMgmtBaseController : AdminBaseController
{
    protected abstract string ModuleKey { get; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var module = FeeMgmtModuleCatalog.Get(ModuleKey);
        ViewData["FeeMgmtModuleKey"] = ModuleKey;
        ViewData["FeeMgmtModule"] = module;
        base.OnActionExecuting(context);
    }
}
