using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/programs")]
public sealed class StudentProgramsController : StudentMgmtBaseController
{
    private readonly IProgramRepository _programs;

    public StudentProgramsController(IProgramRepository programs)
    {
        _programs = programs;
    }

    protected override string ModuleKey => "Programs";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "All Programs";
        ViewData["PageTitle"] = "Programs · All Programs";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _programs.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Program";
        ViewData["PageTitle"] = "Programs · Add";
        return View(new ProgramFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramFormModel form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Program";
        ViewData["PageTitle"] = "Programs · Add";

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        if (await _programs.ProgramCodeExistsAsync(form.ProgramCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.ProgramCode), "Program code already exists.");
            return View(form);
        }

        var newId = await _programs.InsertAsync(form, cancellationToken);
        TempData["StatusMessage"] = $"Program created (id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _programs.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Program";
        ViewData["PageTitle"] = "Programs · Edit";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgramFormModel form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Program";
        ViewData["PageTitle"] = "Programs · Edit";

        if (id != form.ProgramId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        if (await _programs.ProgramCodeExistsAsync(form.ProgramCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.ProgramCode), "Program code already exists.");
            return View(form);
        }

        var ok = await _programs.UpdateAsync(form, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Program updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var ok = await _programs.SetActiveAsync(id, isActive: false, cancellationToken);
        TempData["StatusMessage"] = ok ? "Program deactivated." : "Program not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("activate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var ok = await _programs.SetActiveAsync(id, isActive: true, cancellationToken);
        TempData["StatusMessage"] = ok ? "Program activated." : "Program not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _programs.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok
                ? "Program deleted permanently."
                : "Program not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] =
                "Program could not be deleted because enrollments or other records still reference it. Deactivate it instead, or remove those links first.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("details/{id:int}")]
    public IActionResult Details(int id) =>
        Redirect("/adminportal/settings/courses");

    [HttpGet("courses")]
    public IActionResult Courses() =>
        Redirect("/adminportal/settings/courses");
}
