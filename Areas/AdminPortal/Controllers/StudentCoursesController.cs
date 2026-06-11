using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/courses")]
public sealed class StudentCoursesController : StudentMgmtBaseController
{
    private readonly ICourseRepository _courses;

    public StudentCoursesController(ICourseRepository courses)
    {
        _courses = courses;
    }

    protected override string ModuleKey => "Courses";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "All Courses";
        ViewData["PageTitle"] = "Courses · All Courses";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _courses.ListAsync(search, activeOnly: !showInactive, cancellationToken: cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? programId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Course";
        ViewData["PageTitle"] = "Courses · Add";

        var lookups = await _courses.GetLookupsAsync(cancellationToken);
        var form = new CourseFormModel
        {
            ProgramId = programId is > 0 && lookups.Programs.Any(p => p.Id == programId.Value)
                ? programId.Value
                : lookups.Programs.FirstOrDefault()?.Id ?? 0,
            CreditHours = 3,
            IsMandatory = true,
            IsActive = true
        };

        return View(new CourseFormPageViewModel
        {
            Lookups = lookups,
            Form = form
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Course";
        ViewData["PageTitle"] = "Courses · Add";

        await ValidateCourseFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _courses.CourseCodeExistsAsync(model.Form.CourseCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseCode), "Course code already exists.");
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _courses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Course created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(nameof(model.Form.ProgramId), "Select a valid program.");
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _courses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Course";
        ViewData["PageTitle"] = "Courses · Edit";

        return View(new CourseFormPageViewModel
        {
            Form = row,
            Lookups = await _courses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Course";
        ViewData["PageTitle"] = "Courses · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateCourseFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _courses.CourseCodeExistsAsync(model.Form.CourseCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseCode), "Course code already exists.");
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _courses.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Course updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(nameof(model.Form.ProgramId), "Select a valid program.");
            model.Lookups = await _courses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _courses.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Course deactivated." : "Course not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCourseFormAsync(CourseFormModel form, CancellationToken cancellationToken)
    {
        var lookups = await _courses.GetLookupsAsync(cancellationToken);
        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }
    }

    private void ApplyUniqueConstraintError(SqlException ex, CourseFormPageViewModel model)
    {
        if (ex.Message.Contains("CourseCode", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("UQ_Courses_Code", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.CourseCode), "Course code already exists.");
            return;
        }

        ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
    }
}
