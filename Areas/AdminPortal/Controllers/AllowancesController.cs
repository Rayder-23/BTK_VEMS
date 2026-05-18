using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/allowances")]
public sealed class AllowancesController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Allowances";
}
