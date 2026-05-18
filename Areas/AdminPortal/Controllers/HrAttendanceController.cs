using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/attendance")]
public sealed class HrAttendanceController : HrPlaceholderModuleControllerBase
{
    protected override string ModuleKey => "Attendance";
}
