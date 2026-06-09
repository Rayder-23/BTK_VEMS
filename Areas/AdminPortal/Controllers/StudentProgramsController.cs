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
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Program";
        ViewData["PageTitle"] = "Programs · Add";

        var lookups = await _programs.GetLookupsAsync(cancellationToken);
        return View(new ProgramFormPageViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Program";
        ViewData["PageTitle"] = "Programs · Add";

        await ValidateProgramFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _programs.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _programs.ProgramCodeExistsAsync(model.Form.ProgramCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ProgramCode), "Program code already exists.");
            model.Lookups = await _programs.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var newId = await _programs.InsertAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
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

        return View(new ProgramFormPageViewModel
        {
            Form = row,
            Lookups = await _programs.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgramFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Program";
        ViewData["PageTitle"] = "Programs · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateProgramFormAsync(model.Form, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _programs.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _programs.ProgramCodeExistsAsync(model.Form.ProgramCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ProgramCode), "Program code already exists.");
            model.Lookups = await _programs.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var ok = await _programs.UpdateAsync(model.Form, cancellationToken);
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

    private async Task ValidateProgramFormAsync(ProgramFormModel form, CancellationToken cancellationToken)
    {
        var lookups = await _programs.GetLookupsAsync(cancellationToken);

        if (lookups.InstitutionTypes.All(t => t.Id != form.InstTypeId))
        {
            ModelState.AddModelError(nameof(form.InstTypeId), "Select a valid institution type.");
        }

        form.ProgramLevel = MatchOptionalConfigValue(
            form.ProgramLevel, lookups.ProgramLevels, nameof(form.ProgramLevel));
        form.ProgramType = MatchOptionalConfigValue(
            form.ProgramType, lookups.ProgramTypes, nameof(form.ProgramType));
        form.DegreeLevel = MatchOptionalConfigValue(
            form.DegreeLevel, lookups.DegreeLevels, nameof(form.DegreeLevel));

        var matchedStatus = lookups.ProgramStatuses.FirstOrDefault(s =>
            string.Equals(s, form.Status, StringComparison.OrdinalIgnoreCase));
        if (matchedStatus is null)
        {
            ModelState.AddModelError(nameof(form.Status), "Select a valid status from Configurations.");
        }
        else
        {
            form.Status = matchedStatus;
        }
    }

    private string? MatchOptionalConfigValue(
        string? value,
        IReadOnlyList<string> allowed,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var matched = allowed.FirstOrDefault(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
        if (matched is null)
        {
            ModelState.AddModelError(fieldName, "Select a valid value from Configurations.");
            return value;
        }

        return matched;
    }

    private static ProgramFormModel CreateDefaultForm(ProgramLookups lookups) => new()
    {
        InstTypeId = lookups.InstitutionTypes.FirstOrDefault()?.Id ?? 0,
        Status = lookups.ProgramStatuses.FirstOrDefault() ?? string.Empty,
        IsActive = true
    };
}
