using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/payroll")]
public sealed class PayrollController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Payroll";
}
