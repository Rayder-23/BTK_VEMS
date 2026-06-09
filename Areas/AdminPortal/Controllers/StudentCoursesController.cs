using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/courses")]
public sealed class StudentCoursesController : StudentMgmtBaseController
{
    private readonly ICourseRepository _courses;

    public StudentCoursesController(ICourseRepository courses)
    {
        _courses = courses;
    }

    protected override string ModuleKey => "Courses";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "All Courses";
        ViewData["PageTitle"] = "Courses · All Courses";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _courses.ListAsync(search, activeOnly: !showInactive, cancellationToken: cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? programId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Course";
        ViewData["PageTitle"] = "Courses · Add";

        var lookups = await _courses.GetLookupsAsync(null, cancellationToken);
        var form = CreateDefaultForm(lookups);
        if (programId.HasValue && lookups.Programs.Any(p => p.Id == programId.Value))
        {
            form.ProgramId = programId.Value;
        }

        return View(new CourseFormPageViewModel
        {
            Lookups = lookups,
            Form = form
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Course";
        ViewData["PageTitle"] = "Courses · Add";

        await ValidateCourseFormAsync(model.Form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _courses.GetLookupsAsync(null, cancellationToken);
            return View(model);
        }

        if (await _courses.CourseCodeExistsAsync(model.Form.CourseCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseCode), "Course code already exists.");
            model.Lookups = await _courses.GetLookupsAsync(null, cancellationToken);
            return View(model);
        }

        var newId = await _courses.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Course created (id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _courses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Course";
        ViewData["PageTitle"] = "Courses · Edit";

        return View(new CourseFormPageViewModel
        {
            Form = row,
            Lookups = await _courses.GetLookupsAsync(id, cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Course";
        ViewData["PageTitle"] = "Courses · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateCourseFormAsync(model.Form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _courses.GetLookupsAsync(id, cancellationToken);
            return View(model);
        }

        if (await _courses.CourseCodeExistsAsync(model.Form.CourseCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.CourseCode), "Course code already exists.");
            model.Lookups = await _courses.GetLookupsAsync(id, cancellationToken);
            return View(model);
        }

        var ok = await _courses.UpdateAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _courses.DeactivateAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Course deactivated." : "Course not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCourseFormAsync(CourseFormModel form, int? courseUid, CancellationToken cancellationToken)
    {
        var lookups = await _courses.GetLookupsAsync(courseUid, cancellationToken);

        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        var matchedType = lookups.CourseTypes.FirstOrDefault(t =>
            string.Equals(t, form.CourseType, StringComparison.OrdinalIgnoreCase));
        if (matchedType is null)
        {
            ModelState.AddModelError(nameof(form.CourseType), "Select a valid course type from Configurations.");
        }
        else
        {
            form.CourseType = matchedType;
        }

        var matchedLevel = lookups.CourseLevels.FirstOrDefault(l =>
            string.Equals(l, form.CourseLevel, StringComparison.OrdinalIgnoreCase));
        if (matchedLevel is null)
        {
            ModelState.AddModelError(nameof(form.CourseLevel), "Select a valid course level from Configurations.");
        }
        else
        {
            form.CourseLevel = matchedLevel;
        }

        if (form.PrerequisiteCourseId.HasValue
            && lookups.PrerequisiteCourses.All(c => c.Id != form.PrerequisiteCourseId.Value))
        {
            ModelState.AddModelError(nameof(form.PrerequisiteCourseId), "Select a valid prerequisite course.");
        }

        if (courseUid.HasValue && form.PrerequisiteCourseId == courseUid)
        {
            ModelState.AddModelError(nameof(form.PrerequisiteCourseId), "A course cannot be its own prerequisite.");
        }
    }

    private static CourseFormModel CreateDefaultForm(CourseLookups lookups) => new()
    {
        ProgramId = lookups.Programs.FirstOrDefault()?.Id ?? 0,
        CourseType = lookups.CourseTypes.FirstOrDefault() ?? string.Empty,
        CourseLevel = lookups.CourseLevels.FirstOrDefault() ?? string.Empty,
        CreditHours = 3,
        IsActive = true
    };

    private int ResolveActorId() => ResolveStaffLoginUid() ?? 1;
}
