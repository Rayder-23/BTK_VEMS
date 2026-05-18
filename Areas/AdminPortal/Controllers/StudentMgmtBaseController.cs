using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public abstract class StudentMgmtBaseController : AdminBaseController
{
    protected abstract string ModuleKey { get; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var module = StudentMgmtModuleCatalog.Get(ModuleKey);
        ViewData["StudentMgmtModuleKey"] = ModuleKey;
        ViewData["StudentMgmtModule"] = module;
        base.OnActionExecuting(context);
    }
}
