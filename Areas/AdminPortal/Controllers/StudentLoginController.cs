using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/login")]
public sealed class StudentLoginController : StudentMgmtBaseController
{
    private readonly IStudentsLoginRepository _logins;

    public StudentLoginController(IStudentsLoginRepository logins)
    {
        _logins = logins;
    }

    protected override string ModuleKey => "CreateLogin";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "All Logins";
        ViewData["PageTitle"] = "Create Login · All Logins";
        var items = await _logins.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Login";
        ViewData["PageTitle"] = "Create Login · Add";

        var students = await _logins.GetStudentsWithoutLoginAsync(cancellationToken);
        return View(new StudentLoginFormViewModel
        {
            AvailableStudents = students,
            Form = new StudentLoginFormModel
            {
                Status = "Active",
                MustChangePassword = true
            }
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentLoginFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Login";
        ViewData["PageTitle"] = "Create Login · Add";

        model.AvailableStudents = await _logins.GetStudentsWithoutLoginAsync(cancellationToken);
        ClearPasswordValidationIfDefault(model, ModelState);

        if (!ModelState.IsValid || !await ValidateUsernameAsync(model.Form, cancellationToken))
        {
            return View(model);
        }

        var password = ResolvePassword(model.Form.Password, isCreate: true);

        try
        {
            var newId = await _logins.InsertAsync(model.Form, password, ResolveStaffLoginUid(), cancellationToken);
            TempData["StatusMessage"] =
                $"Student login created (id {newId}). Initial password: {StudentLoginFormViewModel.DefaultPassword} (unless you set a custom password).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.StudentId), "This student already has a login account.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Login";
        ViewData["PageTitle"] = "Create Login · Edit";

        var row = await _logins.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return View(new StudentLoginFormViewModel { Form = row });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentLoginFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Login";
        ViewData["PageTitle"] = "Create Login · Edit";

        model.Form.Uid = id;
        ClearPasswordValidationIfDefault(model, ModelState, isEdit: true);

        if (!ModelState.IsValid || !await ValidateUsernameAsync(model.Form, cancellationToken))
        {
            var existing = await _logins.GetAsync(id, cancellationToken);
            if (existing is not null)
            {
                model.Form.StudentDisplayName = existing.StudentDisplayName;
                model.Form.RegistrationNo = existing.RegistrationNo;
            }

            return View(model);
        }

        var password = ResolvePassword(model.Form.Password, isCreate: false);
        var ok = await _logins.UpdateAsync(model.Form, password, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = string.IsNullOrWhiteSpace(model.Form.Password)
            ? "Login updated."
            : "Login updated and password changed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateUsernameAsync(StudentLoginFormModel form, CancellationToken cancellationToken)
    {
        if (await _logins.UsernameExistsAsync(form.Username, form.Uid > 0 ? form.Uid : null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.Username), "This username is already in use.");
            return false;
        }

        return true;
    }

    private static string ResolvePassword(string? password, bool isCreate)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            return password.Trim();
        }

        return isCreate ? StudentLoginFormViewModel.DefaultPassword : string.Empty;
    }

    private static void ClearPasswordValidationIfDefault(
        StudentLoginFormViewModel model,
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
