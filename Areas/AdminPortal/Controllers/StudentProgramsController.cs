using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/programs")]
public sealed class StudentProgramsController : StudentMgmtBaseController
{
    private readonly IProgramRepository _programs;
    private readonly ICourseRepository _courses;

    public StudentProgramsController(IProgramRepository programs, ICourseRepository courses)
    {
        _programs = programs;
        _courses = courses;
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

        if (id != form.Uid)
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

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _programs.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Program deactivated." : "Program not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("details/{id:int}")]
    public Task<IActionResult> Details(
        int id,
        string? search,
        bool showInactive = false,
        CancellationToken cancellationToken = default) =>
        ProgramCoursesAsync(id, search, showInactive, cancellationToken);

    [HttpGet("courses")]
    public Task<IActionResult> Courses(
        int? programId,
        string? search,
        bool showInactive = false,
        CancellationToken cancellationToken = default) =>
        ProgramCoursesAsync(programId, search, showInactive, cancellationToken);

    private async Task<IActionResult> ProgramCoursesAsync(
        int? programId,
        string? search,
        bool showInactive,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = programId.HasValue ? "Program Courses" : "Courses by Program";
        ViewData["PageTitle"] = programId.HasValue ? "Programs · Courses" : "Programs · Select Program";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var model = new ProgramCoursesPageViewModel
        {
            SelectedProgramId = programId,
            Search = search,
            ShowInactive = showInactive,
            Programs = await _programs.GetProgramOptionsAsync(activeOnly: false, cancellationToken)
        };

        if (programId.HasValue)
        {
            model.ProgramSummary = await _programs.GetListItemAsync(programId.Value, cancellationToken);
            model.Program = await _programs.GetAsync(programId.Value, cancellationToken);
            if (model.Program is null)
            {
                return NotFound();
            }

            model.Courses = await _courses.ListAsync(
                search,
                activeOnly: !showInactive,
                programId: programId.Value,
                cancellationToken);
        }

        return View("ProgramCourses", model);
    }
}
