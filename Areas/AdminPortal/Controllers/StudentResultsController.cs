using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/results")]
public sealed class StudentResultsController : StudentMgmtPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Results";
}
