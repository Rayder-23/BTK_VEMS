using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/fee")]
public sealed class StudentFeeController : StudentMgmtPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Fee";
}
