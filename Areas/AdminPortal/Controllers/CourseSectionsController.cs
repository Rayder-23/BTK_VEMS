using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/course-sections")]
public sealed class CourseSectionsController : AdminBaseController
{
    private readonly ICourseSectionRepository _courseSections;

    public CourseSectionsController(ICourseSectionRepository courseSections)
    {
        _courseSections = courseSections;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Uni course sections";
        ViewData["PageTitle"] = "Settings · Uni course sections";
        ViewData["Search"] = search;

        var items = await _courseSections.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add course section";
        ViewData["PageTitle"] = "Settings · Add course section";

        return View(new CourseSectionFormPageViewModel
        {
            Lookups = await _courseSections.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseSectionFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add course section";
        ViewData["PageTitle"] = "Settings · Add course section";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _courseSections.ExistsAsync(
                model.Form.AcademicYearId,
                model.Form.CourseId,
                model.Form.SectionName,
                null,
                cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This academic year, course, and section name combination already exists.");
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _courseSections.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Course section created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected academic year or course is invalid."
                : "This academic year, course, and section name combination already exists.");
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _courseSections.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit course section";
        ViewData["PageTitle"] = "Settings · Edit course section";

        return View(new CourseSectionFormPageViewModel
        {
            Form = row,
            Lookups = await _courseSections.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseSectionFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit course section";
        ViewData["PageTitle"] = "Settings · Edit course section";

        if (id != model.Form.CourseSectionId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _courseSections.ExistsAsync(
                model.Form.AcademicYearId,
                model.Form.CourseId,
                model.Form.SectionName,
                id,
                cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This academic year, course, and section name combination already exists.");
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _courseSections.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Course section updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected academic year or course is invalid."
                : "This academic year, course, and section name combination already exists.");
            model.Lookups = await _courseSections.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _courseSections.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Course section deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This course section cannot be deleted because student registrations still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
