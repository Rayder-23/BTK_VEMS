using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

[Area("StudentPortal")]
[Authorize]
public abstract class StudentPortalBaseController : Controller
{
    protected async Task<int?> ResolveStudentUidAsync(IStudentProfileRepository profiles, CancellationToken cancellationToken)
    {
        var studentIdClaim = User.FindFirst("StudentId")?.Value;
        if (int.TryParse(studentIdClaim, out var studentUid))
        {
            return studentUid;
        }

        var loginUidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(loginUidClaim, out var loginUid))
        {
            return await profiles.ResolveStudentUidByLoginUidAsync(loginUid, cancellationToken);
        }

        return null;
    }
}
