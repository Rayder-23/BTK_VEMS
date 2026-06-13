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
    public IActionResult Index(string? search, CancellationToken cancellationToken = default) =>
        RedirectPermanent("/adminportal/students/students");

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Assign course to class section";
        ViewData["PageTitle"] = "Class Section Courses · Assign";

        return View(new ClassCourseFormPageViewModel
        {
            Lookups = await _classCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Assign course to class section";
        ViewData["PageTitle"] = "Class Section Courses · Assign";

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classCourses.ExistsAsync(model.Form.ClassSectionId, model.Form.CourseId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class section.");
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _classCourses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class section course link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class section.");
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

        ViewData["Title"] = "Edit class section course";
        ViewData["PageTitle"] = "Class Section Courses · Edit";

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
        ViewData["Title"] = "Edit class section course";
        ViewData["PageTitle"] = "Class Section Courses · Edit";

        if (id != model.Form.ClassSectionCourseId)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _classCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classCourses.ExistsAsync(model.Form.ClassSectionId, model.Form.CourseId, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class section.");
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

            TempData["StatusMessage"] = "Class section course link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "This course is already linked to the selected class section.");
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
        try
        {
            var ok = await _classCourses.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Class section course link deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This link could not be deleted because enrollments or teacher assignments still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(ClassCourseFormModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _classCourses.GetLookupsAsync(cancellationToken);

        if (lookups.ClassSections.All(c => c.Id != form.ClassSectionId))
        {
            ModelState.AddModelError(nameof(form.ClassSectionId), "Select a valid class section.");
        }

        if (lookups.Courses.All(c => c.Id != form.CourseId))
        {
            ModelState.AddModelError(nameof(form.CourseId), "Select a valid course.");
        }
    }

    private void ApplyForeignKeyError(SqlException ex, ClassCourseFormPageViewModel model)
    {
        if (ex.Message.Contains("FK_ClassSectionCourses_ClassSection", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.ClassSectionId), "Select a valid class section.");
            return;
        }

        if (ex.Message.Contains("FK_ClassSectionCourses_Course", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.CourseId), "Select a valid course.");
        }
    }
}
