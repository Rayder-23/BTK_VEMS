using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/courses")]
public sealed class StudentCoursesController : AdminBaseController
{
    private readonly ICourseRepository _courses;

    public StudentCoursesController(ICourseRepository courses)
    {
        _courses = courses;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Courses";
        ViewData["PageTitle"] = "Settings · Courses";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _courses.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add course";
        ViewData["PageTitle"] = "Settings · Add course";

        return View(new CourseFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormModel form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add course";
        ViewData["PageTitle"] = "Settings · Add course";

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        if (!string.IsNullOrWhiteSpace(form.CourseCode)
            && await _courses.CourseCodeExistsAsync(form.CourseCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.CourseCode), "Course code already exists.");
            return View(form);
        }

        try
        {
            var newId = await _courses.InsertAsync(form, cancellationToken);
            TempData["StatusMessage"] = $"Course created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, form);
            return View(form);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _courses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit course";
        ViewData["PageTitle"] = "Settings · Edit course";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormModel form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit course";
        ViewData["PageTitle"] = "Settings · Edit course";

        if (id != form.CourseId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        if (!string.IsNullOrWhiteSpace(form.CourseCode)
            && await _courses.CourseCodeExistsAsync(form.CourseCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.CourseCode), "Course code already exists.");
            return View(form);
        }

        try
        {
            var ok = await _courses.UpdateAsync(form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Course updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, form);
            return View(form);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _courses.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Course deactivated." : "Course not found.";
        return RedirectToAction(nameof(Index));
    }

    private void ApplyUniqueConstraintError(SqlException ex, CourseFormModel form)
    {
        if (ex.Message.Contains("CourseCode", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(form.CourseCode), "Course code already exists.");
            return;
        }

        ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
    }
}
