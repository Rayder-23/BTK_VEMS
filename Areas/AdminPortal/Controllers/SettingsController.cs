using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings")]
public sealed class SettingsController : AdminBaseController
{
    private readonly IConfigurationsRepository _configurations;

    public SettingsController(IConfigurationsRepository configurations)
    {
        _configurations = configurations;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Settings";
        ViewData["PageTitle"] = "Settings";
        var items = await _configurations.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add configuration";
        ViewData["PageTitle"] = "Settings · Add";
        return View(new ConfigurationFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConfigurationFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add configuration";
        ViewData["PageTitle"] = "Settings · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newId = await _configurations.InsertAsync(model, cancellationToken);
        TempData["StatusMessage"] = $"Configuration created (internal id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit configuration";
        ViewData["PageTitle"] = "Settings · Edit";

        var row = await _configurations.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ConfigurationFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit configuration";
        ViewData["PageTitle"] = "Settings · Edit";

        if (model.Uid <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ok = await _configurations.UpdateAsync(model, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Configuration updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _configurations.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok
                ? "Configuration deleted."
                : "Configuration could not be deleted (record not found).";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This row cannot be deleted because other data still references it.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("set-active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        var ok = await _configurations.UpdateIsActiveAsync(id, isActive, cancellationToken);
        TempData["StatusMessage"] = ok
            ? $"Configuration {(isActive ? "activated" : "deactivated")}."
            : "Configuration could not be updated (record not found).";
        return RedirectToAction(nameof(Index));
    }
}
