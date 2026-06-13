using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/academic-years")]
public sealed class AcademicYearsController : AdminBaseController
{
    private readonly IAcademicYearRepository _academicYears;

    public AcademicYearsController(IAcademicYearRepository academicYears)
    {
        _academicYears = academicYears;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Academic years";
        ViewData["PageTitle"] = "Settings · Academic years";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _academicYears.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add academic year";
        ViewData["PageTitle"] = "Settings · Add academic year";
        return View(new AcademicYearFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AcademicYearFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add academic year";
        ViewData["PageTitle"] = "Settings · Add academic year";

        ValidateForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _academicYears.NameExistsAsync(model.YearName, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.YearName), "An academic year with this name already exists.");
            return View(model);
        }

        try
        {
            var newId = await _academicYears.InsertAsync(model, cancellationToken);
            TempData["StatusMessage"] = $"Academic year created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.YearName), "An academic year with this name already exists.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _academicYears.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit academic year";
        ViewData["PageTitle"] = "Settings · Edit academic year";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AcademicYearFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit academic year";
        ViewData["PageTitle"] = "Settings · Edit academic year";

        if (id != model.AcademicYearId)
        {
            return NotFound();
        }

        ValidateForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _academicYears.NameExistsAsync(model.YearName, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.YearName), "An academic year with this name already exists.");
            return View(model);
        }

        try
        {
            var ok = await _academicYears.UpdateAsync(model, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Academic year updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.YearName), "An academic year with this name already exists.");
            return View(model);
        }
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var ok = await _academicYears.SetActiveAsync(id, isActive: false, cancellationToken);
        TempData["StatusMessage"] = ok ? "Academic year deactivated." : "Academic year not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("activate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var ok = await _academicYears.SetActiveAsync(id, isActive: true, cancellationToken);
        TempData["StatusMessage"] = ok ? "Academic year activated." : "Academic year not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _academicYears.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok
                ? "Academic year deleted."
                : "Academic year could not be deleted (record not found).";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This academic year cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(AcademicYearFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date cannot be earlier than start date.");
        }
    }
}
