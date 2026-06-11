using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/classes")]
public sealed class ClassesController : StudentMgmtBaseController
{
    private readonly IClassRepository _classes;

    public ClassesController(IClassRepository classes)
    {
        _classes = classes;
    }

    protected override string ModuleKey => "Classes";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Classes";
        ViewData["PageTitle"] = "Classes · All";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _classes.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class";
        ViewData["PageTitle"] = "Classes · Add";

        var lookups = await _classes.GetLookupsAsync(cancellationToken);
        return View(new ClassFormPageViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class";
        ViewData["PageTitle"] = "Classes · Add";

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classes.ClassCodeExistsAsync(model.Form.ClassCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _classes.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(nameof(model.Form.ProgramId), "Select a valid program.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _classes.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit class";
        ViewData["PageTitle"] = "Classes · Edit";

        return View(new ClassFormPageViewModel
        {
            Form = row,
            Lookups = await _classes.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit class";
        ViewData["PageTitle"] = "Classes · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classes.ClassCodeExistsAsync(model.Form.ClassCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _classes.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Class updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(nameof(model.Form.ProgramId), "Select a valid program.");
            model.Lookups = await _classes.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _classes.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Class deactivated." : "Class not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(ClassFormModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _classes.GetLookupsAsync(cancellationToken);
        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        var semester = ClassFieldCatalog.ResolveSemester(form.Semester);
        if (semester is null)
        {
            ModelState.AddModelError(nameof(form.Semester), "Select a valid semester.");
        }
        else
        {
            form.Semester = semester;
        }

        if (!string.IsNullOrWhiteSpace(form.Shift))
        {
            var shift = ClassFieldCatalog.ResolveShift(form.Shift);
            if (shift is null)
            {
                ModelState.AddModelError(nameof(form.Shift), "Select a valid shift.");
            }
            else
            {
                form.Shift = shift;
            }
        }
    }

    private static ClassFormModel CreateDefaultForm(ClassLookups lookups) => new()
    {
        ProgramId = lookups.Programs.FirstOrDefault()?.Id ?? 0,
        Semester = lookups.Semesters.FirstOrDefault() ?? ClassFieldCatalog.AllowedSemesters[0],
        AcademicYear = (short)DateTime.Today.Year,
        SemesterNo = 1,
        MaxStrength = 40,
        IsActive = true
    };
}
