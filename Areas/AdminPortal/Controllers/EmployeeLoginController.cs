using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/employee-login")]
public sealed class EmployeeLoginController : HrBaseController
{
    private readonly IEmployeeLoginRepository _logins;

    public EmployeeLoginController(IEmployeeLoginRepository logins)
    {
        _logins = logins;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Employee Logins";
        ViewData["PageTitle"] = "Create Login · All Logins";
        var items = await _logins.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Employee Login";
        ViewData["PageTitle"] = "Create Login · Add";

        var lookups = await _logins.GetLookupsAsync(cancellationToken);
        return View(new EmployeeLoginFormViewModel
        {
            AvailableEmployees = await _logins.GetEmployeesWithoutLoginAsync(cancellationToken),
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeLoginFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Employee Login";
        ViewData["PageTitle"] = "Create Login · Add";

        model.AvailableEmployees = await _logins.GetEmployeesWithoutLoginAsync(cancellationToken);
        model.Lookups = await _logins.GetLookupsAsync(cancellationToken);
        ClearPasswordValidationIfEmpty(model, ModelState);

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _logins.UsernameExistsAsync(model.Form.Username, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.Username), "This username is already in use.");
            return View(model);
        }

        var password = ResolvePassword(model.Form.Password, isCreate: true);

        try
        {
            var newId = await _logins.InsertAsync(model.Form, password, ResolveStaffLoginUid(), cancellationToken);
            TempData["StatusMessage"] =
                $"Employee login created (id {newId}). Initial password: {EmployeeLoginFormViewModel.DefaultPassword} (unless you set a custom password).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.EmployeeUid), "This employee already has a login account.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _logins.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Employee Login";
        ViewData["PageTitle"] = "Create Login · Edit";

        return View(new EmployeeLoginFormViewModel
        {
            Form = row,
            Lookups = await _logins.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeLoginFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Employee Login";
        ViewData["PageTitle"] = "Create Login · Edit";

        model.Form.Uid = id;
        model.Lookups = await _logins.GetLookupsAsync(cancellationToken);
        ClearPasswordValidationIfEmpty(model, ModelState, isEdit: true);

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            var existing = await _logins.GetAsync(id, cancellationToken);
            if (existing is not null)
            {
                model.Form.EmployeeDisplayName = existing.EmployeeDisplayName;
                model.Form.EmployeeCode = existing.EmployeeCode;
                model.Form.EmployeeUid = existing.EmployeeUid;
            }

            return View(model);
        }

        if (await _logins.UsernameExistsAsync(model.Form.Username, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.Username), "This username is already in use.");
            return View(model);
        }

        var password = ResolvePassword(model.Form.Password, isCreate: false);
        var ok = await _logins.UpdateAsync(model.Form, string.IsNullOrWhiteSpace(password) ? null : password, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = string.IsNullOrWhiteSpace(model.Form.Password)
            ? "Employee login updated."
            : "Employee login updated and password changed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(EmployeeLoginFormModel form, CancellationToken cancellationToken)
    {
        var lookups = await _logins.GetLookupsAsync(cancellationToken);

        var matchedRole = lookups.Roles.FirstOrDefault(r =>
            string.Equals(r, form.Role, StringComparison.OrdinalIgnoreCase));
        if (matchedRole is null)
        {
            ModelState.AddModelError(nameof(form.Role), "Select a valid role from Configurations.");
        }
        else
        {
            form.Role = matchedRole;
        }

        var matchedStatus = lookups.Statuses.FirstOrDefault(s =>
            string.Equals(s, form.Status, StringComparison.OrdinalIgnoreCase));
        if (matchedStatus is null)
        {
            ModelState.AddModelError(nameof(form.Status), "Select a valid status from Configurations.");
        }
        else
        {
            form.Status = matchedStatus;
        }
    }

    private static EmployeeLoginFormModel CreateDefaultForm(EmployeeLoginLookups lookups) => new()
    {
        Role = lookups.Roles.FirstOrDefault() ?? string.Empty,
        Status = lookups.Statuses.FirstOrDefault(s =>
            string.Equals(s, "Active", StringComparison.OrdinalIgnoreCase))
            ?? lookups.Statuses.FirstOrDefault()
            ?? string.Empty
    };

    private static string ResolvePassword(string? password, bool isCreate)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            return password.Trim();
        }

        return isCreate ? EmployeeLoginFormViewModel.DefaultPassword : string.Empty;
    }

    private static void ClearPasswordValidationIfEmpty(
        EmployeeLoginFormViewModel model,
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState,
        bool isEdit = false)
    {
        if (isEdit && string.IsNullOrWhiteSpace(model.Form.Password))
        {
            modelState.Remove(nameof(model.Form.Password));
            return;
        }

        if (string.IsNullOrWhiteSpace(model.Form.Password))
        {
            modelState.Remove(nameof(model.Form.Password));
        }
    }
}
