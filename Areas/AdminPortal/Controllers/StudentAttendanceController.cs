using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/attendance")]
public sealed class StudentAttendanceController : StudentMgmtPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Attendance";
}
