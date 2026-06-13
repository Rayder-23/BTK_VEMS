using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/program-courses")]
public sealed class ProgramCoursesController : AdminBaseController
{
    private readonly IProgramCourseRepository _programCourses;

    public ProgramCoursesController(IProgramCourseRepository programCourses)
    {
        _programCourses = programCourses;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link program courses";
        ViewData["PageTitle"] = "Settings · Link program courses";
        ViewData["Search"] = search;

        var items = await _programCourses.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add program course link";
        ViewData["PageTitle"] = "Settings · Add program course link";

        return View(new ProgramCourseFormPageViewModel
        {
            Lookups = await _programCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add program course link";
        ViewData["PageTitle"] = "Settings · Add program course link";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _programCourses.ExistsAsync(model.Form.ProgramId, model.Form.CourseId, null, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This program and course combination already exists.");
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _programCourses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Program course link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected program or course is invalid."
                : "This program and course combination already exists.");
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _programCourses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit program course link";
        ViewData["PageTitle"] = "Settings · Edit program course link";

        return View(new ProgramCourseFormPageViewModel
        {
            Form = row,
            Lookups = await _programCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgramCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit program course link";
        ViewData["PageTitle"] = "Settings · Edit program course link";

        if (id != model.Form.ProgramCourseId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _programCourses.ExistsAsync(model.Form.ProgramId, model.Form.CourseId, id, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This program and course combination already exists.");
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _programCourses.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Program course link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected program or course is invalid."
                : "This program and course combination already exists.");
            model.Lookups = await _programCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _programCourses.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Program course link deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This link cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
