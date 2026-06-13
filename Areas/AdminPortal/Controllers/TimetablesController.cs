using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/timetables")]
public sealed class TimetablesController : AdminBaseController
{
    private readonly ITimetablesRepository _timetables;

    public TimetablesController(ITimetablesRepository timetables)
    {
        _timetables = timetables;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Timetable";
        ViewData["PageTitle"] = "Settings · Timetable";
        ViewData["Search"] = search;

        var items = await _timetables.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add timetable slot";
        ViewData["PageTitle"] = "Settings · Add timetable slot";

        return View(new TimetableFormPageViewModel
        {
            Lookups = await _timetables.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimetableFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add timetable slot";
        ViewData["PageTitle"] = "Settings · Add timetable slot";

        ValidateForm(model.Form);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _timetables.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _timetables.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Timetable slot created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This timetable slot already exists.");
            model.Lookups = await _timetables.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _timetables.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit timetable slot";
        ViewData["PageTitle"] = "Settings · Edit timetable slot";

        return View(new TimetableFormPageViewModel
        {
            Form = row,
            Lookups = await _timetables.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TimetableFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit timetable slot";
        ViewData["PageTitle"] = "Settings · Edit timetable slot";

        if (id != model.Form.TimetableId)
        {
            return NotFound();
        }

        ValidateForm(model.Form);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _timetables.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _timetables.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Timetable slot updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This timetable slot already exists.");
            model.Lookups = await _timetables.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _timetables.DeleteAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Timetable slot deleted." : "Record not found.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(TimetableFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        if (model.ClassSectionId is null or <= 0 && model.CourseSectionId is null or <= 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one class section or course section.");
        }
    }
}
