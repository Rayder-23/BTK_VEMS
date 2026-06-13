using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/sections")]
public sealed class SectionsController : AdminBaseController
{
    private readonly ISectionRepository _sections;

    public SectionsController(ISectionRepository sections)
    {
        _sections = sections;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Sections";
        ViewData["PageTitle"] = "Settings · Sections";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _sections.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add section";
        ViewData["PageTitle"] = "Settings · Add section";
        return View(new SectionFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectionFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add section";
        ViewData["PageTitle"] = "Settings · Add section";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _sections.NameExistsAsync(model.SectionName, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.SectionName), "Section name already exists.");
            return View(model);
        }

        try
        {
            var newId = await _sections.InsertAsync(model, cancellationToken);
            TempData["StatusMessage"] = $"Section created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.SectionName), "Section name already exists.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _sections.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit section";
        ViewData["PageTitle"] = "Settings · Edit section";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SectionFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit section";
        ViewData["PageTitle"] = "Settings · Edit section";

        if (id != model.SectionId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _sections.NameExistsAsync(model.SectionName, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.SectionName), "Section name already exists.");
            return View(model);
        }

        try
        {
            var ok = await _sections.UpdateAsync(model, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Section updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.SectionName), "Section name already exists.");
            return View(model);
        }
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var ok = await _sections.SetActiveAsync(id, isActive: false, cancellationToken);
        TempData["StatusMessage"] = ok ? "Section deactivated." : "Section not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("activate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var ok = await _sections.SetActiveAsync(id, isActive: true, cancellationToken);
        TempData["StatusMessage"] = ok ? "Section activated." : "Section not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _sections.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Section deleted permanently." : "Section not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] =
                "Section could not be deleted because students or teacher assignments still reference it. Deactivate it instead, or remove those links first.";
        }

        return RedirectToAction(nameof(Index));
    }
}
