using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class ProfileController : TeacherPortalBaseController
{
    private readonly IEmployeeRepository _employees;

    public ProfileController(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var employeeUidClaim = User.FindFirst("EmployeeUid")?.Value;
        if (!int.TryParse(employeeUidClaim, out var employeeUid))
        {
            return RedirectToAction("Index", "Login");
        }

        var employee = await _employees.GetAsync(employeeUid, cancellationToken);
        if (employee is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Profile";
        ViewData["LoginUsername"] = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
        ViewData["LoginRole"] = User.FindFirst(ClaimTypes.Role)?.Value;
        return View(employee);
    }
}
