using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/deductions")]
public sealed class DeductionsController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Deductions";
}
