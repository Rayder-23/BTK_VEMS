using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/periods")]
public sealed class PeriodsController : AdminBaseController
{
    private readonly IPeriodRepository _periods;

    public PeriodsController(IPeriodRepository periods)
    {
        _periods = periods;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Periods";
        ViewData["PageTitle"] = "Settings · Periods";
        ViewData["Search"] = search;

        var items = await _periods.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add period";
        ViewData["PageTitle"] = "Settings · Add period";
        return View(new PeriodFormModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PeriodFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add period";
        ViewData["PageTitle"] = "Settings · Add period";

        ValidateForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var newId = await _periods.InsertAsync(model, cancellationToken);
            TempData["StatusMessage"] = $"Period created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(string.Empty, "Could not save the period. Check the values and try again.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _periods.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit period";
        ViewData["PageTitle"] = "Settings · Edit period";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PeriodFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit period";
        ViewData["PageTitle"] = "Settings · Edit period";

        if (id != model.PeriodId)
        {
            return NotFound();
        }

        ValidateForm(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var ok = await _periods.UpdateAsync(model, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Period updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException)
        {
            ModelState.AddModelError(string.Empty, "Could not save the period. Check the values and try again.");
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _periods.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Period deleted." : "Period not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This period cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(PeriodFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime <= model.StartTime)
        {
            ModelState.AddModelError(nameof(model.EndTime), "End time must be later than start time.");
        }
    }
}
