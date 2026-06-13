using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/class-section-courses")]
public sealed class ClassSectionCourseLinksController : AdminBaseController
{
    private readonly IClassCourseRepository _classSectionCourses;

    public ClassSectionCourseLinksController(IClassCourseRepository classSectionCourses)
    {
        _classSectionCourses = classSectionCourses;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link class section courses";
        ViewData["PageTitle"] = "Settings · Link class section courses";
        ViewData["Search"] = search;

        var items = await _classSectionCourses.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class section course link";
        ViewData["PageTitle"] = "Settings · Add class section course link";

        return View(new ClassCourseFormPageViewModel
        {
            Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class section course link";
        ViewData["PageTitle"] = "Settings · Add class section course link";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classSectionCourses.ExistsAsync(model.Form.ClassSectionId, model.Form.CourseId, null, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This class section and course combination already exists.");
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _classSectionCourses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class section course link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected class section or course is invalid."
                : "This class section and course combination already exists.");
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _classSectionCourses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit class section course link";
        ViewData["PageTitle"] = "Settings · Edit class section course link";

        return View(new ClassCourseFormPageViewModel
        {
            Form = row,
            Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit class section course link";
        ViewData["PageTitle"] = "Settings · Edit class section course link";

        if (id != model.Form.ClassSectionCourseId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classSectionCourses.ExistsAsync(model.Form.ClassSectionId, model.Form.CourseId, id, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This class section and course combination already exists.");
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _classSectionCourses.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Class section course link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected class section or course is invalid."
                : "This class section and course combination already exists.");
            model.Lookups = await _classSectionCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _classSectionCourses.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Class section course link deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This link cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
