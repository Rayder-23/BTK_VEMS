using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class AcademicManagementController : TeacherPortalBaseController
{
    private readonly IClassRepository _classes;

    public AcademicManagementController(IClassRepository classes)
    {
        _classes = classes;
    }

    [HttpGet]
    public async Task<IActionResult> Classes(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Classes";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _classes.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> CreateClass(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Class";
        var lookups = await _classes.GetLookupsAsync(cancellationToken);
        return View(new ClassFormPageViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClass(ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Class";

        var loginUid = ResolveLoginUid();
        if (loginUid is null)
        {
            return RedirectToAction("Index", "Login");
        }

        await ValidateClassFormAsync(model.Form, null, cancellationToken);
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

        var newId = await _classes.InsertAsync(model.Form, loginUid.Value, cancellationToken);
        TempData["StatusMessage"] = $"Class created (id {newId}).";
        return RedirectToAction(nameof(Classes));
    }

    [HttpGet]
    public async Task<IActionResult> EditClass(int id, CancellationToken cancellationToken)
    {
        var row = await _classes.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Class";
        return View(new ClassFormPageViewModel
        {
            Form = row,
            Lookups = await _classes.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClass(int id, ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Class";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        var loginUid = ResolveLoginUid();
        await ValidateClassFormAsync(model.Form, id, cancellationToken);
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

        var ok = await _classes.UpdateAsync(model.Form, loginUid, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Class updated.";
        return RedirectToAction(nameof(Classes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClass(int id, CancellationToken cancellationToken)
    {
        var ok = await _classes.DeactivateAsync(id, ResolveLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Class deactivated." : "Class not found.";
        return RedirectToAction(nameof(Classes));
    }

    public IActionResult Courses() =>
        Placeholder("Courses", "Manage courses linked to your academic programs.");

    public IActionResult Timetable() =>
        Placeholder("Timetable", "Your weekly teaching schedule will appear here.");

    public IActionResult LessonPlans() =>
        Placeholder("Lesson Plans", "Create and organize lesson plans for your classes.");

    private async Task ValidateClassFormAsync(ClassFormModel form, int? classUid, CancellationToken cancellationToken)
    {
        var lookups = await _classes.GetLookupsAsync(cancellationToken);

        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        var matchedSemester = lookups.Semesters.FirstOrDefault(s =>
            string.Equals(s, form.Semester, StringComparison.OrdinalIgnoreCase));
        if (matchedSemester is null)
        {
            ModelState.AddModelError(nameof(form.Semester), "Select a valid semester.");
        }
        else
        {
            form.Semester = matchedSemester;
        }

        if (!string.IsNullOrWhiteSpace(form.Shift))
        {
            var matchedShift = lookups.Shifts.FirstOrDefault(s =>
                string.Equals(s, form.Shift, StringComparison.OrdinalIgnoreCase));
            if (matchedShift is null)
            {
                ModelState.AddModelError(nameof(form.Shift), "Select a valid shift.");
            }
            else
            {
                form.Shift = matchedShift;
            }
        }
    }

    private static ClassFormModel CreateDefaultForm(ClassLookups lookups) => new()
    {
        ProgramId = lookups.Programs.FirstOrDefault()?.Id ?? 0,
        Semester = lookups.Semesters.FirstOrDefault() ?? string.Empty,
        AcademicYear = (short)DateTime.UtcNow.Year,
        MaxStrength = 40,
        IsActive = true
    };
}
