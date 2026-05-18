using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/challans")]
public sealed class StudentChallansController : StudentMgmtPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Challan";
}
