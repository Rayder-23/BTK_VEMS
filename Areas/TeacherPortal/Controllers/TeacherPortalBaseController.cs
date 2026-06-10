using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
[Authorize(AuthenticationSchemes = TeacherPortalAuth.Scheme)]
public abstract class TeacherPortalBaseController : Controller
{
    protected IActionResult Placeholder(string title, string description)
    {
        ViewData["Title"] = title;
        ViewData["Description"] = description;
        return View("ModulePlaceholder");
    }

    protected int? ResolveLoginUid()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var loginUid) ? loginUid : null;
    }

    protected async Task<int?> ResolveEmployeeUidAsync(
        ITeacherAccountRepository accounts,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirst("EmployeeUid")?.Value, out var employeeUid))
        {
            return employeeUid;
        }

        var loginUid = ResolveLoginUid();
        if (loginUid is null)
        {
            return null;
        }

        return await accounts.GetEmployeeUidByLoginUidAsync(loginUid.Value, cancellationToken);
    }

    protected async Task<int?> ResolveTeacherIdAsync(
        ITeacherAcademicRepository academic,
        ITeacherAccountRepository accounts,
        CancellationToken cancellationToken)
    {
        var employeeUid = await ResolveEmployeeUidAsync(accounts, cancellationToken);
        return await academic.ResolveTeacherIdAsync(
            User.FindFirst("EmployeeId")?.Value,
            employeeUid,
            cancellationToken);
    }
}
