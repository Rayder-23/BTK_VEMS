using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/teachers/teacher-class-courses")]
public sealed class TeacherClassCoursesController : AdminBaseController
{
    private readonly ITeacherClassCourseRepository _links;

    public TeacherClassCoursesController(ITeacherClassCourseRepository links)
    {
        _links = links;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Teacher class courses";
        ViewData["PageTitle"] = "Teacher-Class-Course · All";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _links.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Link teacher to class course";
        ViewData["PageTitle"] = "Teacher-Class-Course · Link";

        return View(new TeacherClassCourseFormPageViewModel
        {
            Lookups = await _links.GetLookupsAsync(cancellationToken),
            Form = new TeacherClassCourseFormModel { IsActive = true, Role = "Lead" }
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Link teacher to class course";
        ViewData["PageTitle"] = "Teacher-Class-Course · Link";

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _links.ExistsAsync(model.Form.TeacherId, model.Form.ClassCourseId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCourseId), "This teacher is already linked to the selected class course.");
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _links.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Teacher class course link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCourseId), "This teacher is already linked to the selected class course.");
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyForeignKeyError(ex, model);
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _links.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit teacher class course link";
        ViewData["PageTitle"] = "Teacher-Class-Course · Edit";

        return View(new TeacherClassCourseFormPageViewModel
        {
            Form = row,
            Lookups = await _links.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit teacher class course link";
        ViewData["PageTitle"] = "Teacher-Class-Course · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _links.ExistsAsync(model.Form.TeacherId, model.Form.ClassCourseId, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCourseId), "This teacher is already linked to the selected class course.");
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _links.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Teacher class course link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCourseId), "This teacher is already linked to the selected class course.");
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyForeignKeyError(ex, model);
            model.Lookups = await _links.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _links.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Teacher class course link deactivated." : "Record not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(TeacherClassCourseFormModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _links.GetLookupsAsync(cancellationToken);

        if (lookups.Teachers.All(t => t.Id != form.TeacherId))
        {
            ModelState.AddModelError(nameof(form.TeacherId), "Select a valid teacher.");
        }

        if (lookups.ClassCourses.All(c => c.Id != form.ClassCourseId))
        {
            ModelState.AddModelError(nameof(form.ClassCourseId), "Select a valid class course.");
        }

        var matchedRole = lookups.Roles.FirstOrDefault(r =>
            string.Equals(r, form.Role, StringComparison.OrdinalIgnoreCase));
        if (matchedRole is null)
        {
            ModelState.AddModelError(nameof(form.Role), "Select a valid role.");
        }
        else
        {
            form.Role = TeacherClassCourseRepository.AllowedRoles.First(r =>
                string.Equals(r, matchedRole, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void ApplyForeignKeyError(SqlException ex, TeacherClassCourseFormPageViewModel model)
    {
        if (ex.Message.Contains("FK_TCC_Teacher", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.TeacherId), "Select a valid teacher.");
            return;
        }

        if (ex.Message.Contains("FK_TCC_ClassCourse", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCourseId), "Select a valid class course.");
        }
    }
}
