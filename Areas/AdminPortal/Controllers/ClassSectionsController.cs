using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/class-sections")]
public sealed class ClassSectionsController : AdminBaseController
{
    private readonly IClassSectionRepository _classSections;

    public ClassSectionsController(IClassSectionRepository classSections)
    {
        _classSections = classSections;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link class sections";
        ViewData["PageTitle"] = "Settings · Link class sections";
        ViewData["Search"] = search;

        var items = await _classSections.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class section link";
        ViewData["PageTitle"] = "Settings · Add class section link";

        return View(new ClassSectionFormPageViewModel
        {
            Lookups = await _classSections.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassSectionFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class section link";
        ViewData["PageTitle"] = "Settings · Add class section link";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classSections.ExistsAsync(
                model.Form.AcademicYearId,
                model.Form.ClassId,
                model.Form.SectionId,
                null,
                cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This class, section, and academic year combination already exists.");
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _classSections.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class section link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected academic year, class, or section is invalid."
                : "This class, section, and academic year combination already exists.");
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _classSections.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit class section link";
        ViewData["PageTitle"] = "Settings · Edit class section link";

        return View(new ClassSectionFormPageViewModel
        {
            Form = row,
            Lookups = await _classSections.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassSectionFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit class section link";
        ViewData["PageTitle"] = "Settings · Edit class section link";

        if (id != model.Form.ClassSectionId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _classSections.ExistsAsync(
                model.Form.AcademicYearId,
                model.Form.ClassId,
                model.Form.SectionId,
                id,
                cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This class, section, and academic year combination already exists.");
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _classSections.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Class section link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected academic year, class, or section is invalid."
                : "This class, section, and academic year combination already exists.");
            model.Lookups = await _classSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _classSections.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Class section link deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This link cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
