using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/class-courses")]
public sealed class ClassCoursesController : StudentMgmtBaseController
{
    private readonly IClassCourseRepository _classCourses;

    public ClassCoursesController(IClassCourseRepository classCourses)
    {
        _classCourses = classCourses;
    }

    protected override string ModuleKey => "ClassCourses";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Class courses";
        ViewData["PageTitle"] = "Class Courses · All";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _classCourses.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Assign course to class";
        ViewData["PageTitle"] = "Class Courses · Assign";

        return View(new ClassCourseFormPageViewModel
        {
            Lookups = await _classCourses.GetLookupsAsync(cancellationToken),
            Form = new ClassCourseFormModel { IsActive = true }
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Assign course to class";
        ViewData["PageTitle"] = "Class Courses · Assign";

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classCourses.ExistsAsync(model.Form.ClassId, model.Form.CourseId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class.");
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _classCourses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class course assignment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class.");
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyForeignKeyError(ex, model);
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _classCourses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit class course";
        ViewData["PageTitle"] = "Class Courses · Edit";

        return View(new ClassCourseFormPageViewModel
        {
            Form = row,
            Lookups = await _classCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit class course";
        ViewData["PageTitle"] = "Class Courses · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classCourses.ExistsAsync(model.Form.ClassId, model.Form.CourseId, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class.");
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _classCourses.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Class course assignment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class.");
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyForeignKeyError(ex, model);
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _classCourses.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Class course assignment deactivated." : "Record not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(ClassCourseFormModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _classCourses.GetLookupsAsync(cancellationToken);

        if (lookups.Classes.All(c => c.Id != form.ClassId))
        {
            ModelState.AddModelError(nameof(form.ClassId), "Select a valid class.");
        }

        if (lookups.Courses.All(c => c.Id != form.CourseId))
        {
            ModelState.AddModelError(nameof(form.CourseId), "Select a valid course.");
        }

        if (form.TeacherId.HasValue && lookups.Teachers.All(t => t.Id != form.TeacherId.Value))
        {
            ModelState.AddModelError(nameof(form.TeacherId), "Select a valid teacher.");
        }
    }

    private void ApplyForeignKeyError(SqlException ex, ClassCourseFormPageViewModel model)
    {
        if (ex.Message.Contains("FK_ClassCourses_Class", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.ClassId), "Select a valid class.");
            return;
        }

        if (ex.Message.Contains("FK_ClassCourses_Course", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "Select a valid course.");
        }
    }
}
