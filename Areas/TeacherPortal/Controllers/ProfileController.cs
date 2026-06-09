using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class ProfileController : TeacherPortalBaseController
{
    private readonly IEmployeeRepository _employees;
    private readonly ITeacherAccountRepository _accounts;

    public ProfileController(IEmployeeRepository employees, ITeacherAccountRepository accounts)
    {
        _employees = employees;
        _accounts = accounts;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var employeeUid = await ResolveEmployeeUidAsync(cancellationToken);
        if (employeeUid is null)
        {
            return RedirectToAction("Index", "Login");
        }

        var employee = await _employees.GetAsync(employeeUid.Value, cancellationToken);
        if (employee is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Profile";
        ViewData["LoginUsername"] = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
        ViewData["LoginRole"] = User.FindFirst(ClaimTypes.Role)?.Value;
        return View(employee);
    }

    private async Task<int?> ResolveEmployeeUidAsync(CancellationToken cancellationToken)
    {
        var employeeUidClaim = User.FindFirst("EmployeeUid")?.Value;
        if (int.TryParse(employeeUidClaim, out var employeeUid))
        {
            return employeeUid;
        }

        var loginUidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(loginUidClaim, out var loginUid))
        {
            return null;
        }

        return await _accounts.GetEmployeeUidByLoginUidAsync(loginUid, cancellationToken);
    }
}
