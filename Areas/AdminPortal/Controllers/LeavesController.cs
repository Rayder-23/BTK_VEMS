using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/leaves")]
public sealed class LeavesController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Leaves";
}
