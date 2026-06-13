using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/classes")]
public sealed class ClassesController : StudentMgmtBaseController
{
    private readonly IClassRepository _classes;

    public ClassesController(IClassRepository classes)
    {
        _classes = classes;
    }

    protected override string ModuleKey => "Classes";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Classes";
        ViewData["PageTitle"] = "Classes · All";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _classes.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add class";
        ViewData["PageTitle"] = "Classes · Add";
        return View(new ClassFormPageViewModel { Form = new ClassFormModel { IsActive = true } });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add class";
        ViewData["PageTitle"] = "Classes · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _classes.ClassCodeExistsAsync(model.Form.ClassCode ?? string.Empty, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            return View(model);
        }

        try
        {
            var newId = await _classes.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Class created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _classes.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit class";
        ViewData["PageTitle"] = "Classes · Edit";

        return View(new ClassFormPageViewModel { Form = row });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit class";
        ViewData["PageTitle"] = "Classes · Edit";

        if (id != model.Form.ClassId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _classes.ClassCodeExistsAsync(model.Form.ClassCode ?? string.Empty, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            return View(model);
        }

        try
        {
            var ok = await _classes.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Class updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassCode), "Class code already exists.");
            return View(model);
        }
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var ok = await _classes.SetActiveAsync(id, isActive: false, cancellationToken);
        TempData["StatusMessage"] = ok ? "Class deactivated." : "Class not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("activate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var ok = await _classes.SetActiveAsync(id, isActive: true, cancellationToken);
        TempData["StatusMessage"] = ok ? "Class activated." : "Class not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _classes.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Class deleted permanently." : "Class not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] =
                "Class could not be deleted because courses, enrollments, or other records still reference it. Deactivate it instead, or remove those links first.";
        }

        return RedirectToAction(nameof(Index));
    }
}
