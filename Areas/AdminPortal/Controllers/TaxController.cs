using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/tax")]
public sealed class TaxController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Tax";
}
