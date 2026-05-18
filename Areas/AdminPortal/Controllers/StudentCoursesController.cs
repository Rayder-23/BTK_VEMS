using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/courses")]
public sealed class StudentCoursesController : StudentMgmtPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Courses";
}
