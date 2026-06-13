using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/teachers/link-teacher-course")]
public sealed class TeacherCoursesController : AdminBaseController
{
    private readonly ITeacherCourseLinkRepository _teacherCourses;

    public TeacherCoursesController(ITeacherCourseLinkRepository teacherCourses)
    {
        _teacherCourses = teacherCourses;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link teacher course";
        ViewData["PageTitle"] = "Teachers · Link teacher course";
        ViewData["Search"] = search;

        var items = await _teacherCourses.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher course link";
        ViewData["PageTitle"] = "Teachers · Link teacher course · Add";

        return View(new TeacherCourseFormPageViewModel
        {
            Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher course link";
        ViewData["PageTitle"] = "Teachers · Link teacher course · Add";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _teacherCourses.ExistsAsync(model.Form.TeacherId, model.Form.CourseId, null, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This teacher and course combination already exists.");
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _teacherCourses.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Teacher course link created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected teacher or course is invalid."
                : "This teacher and course combination already exists.");
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _teacherCourses.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit teacher course link";
        ViewData["PageTitle"] = "Teachers · Link teacher course · Edit";

        return View(new TeacherCourseFormPageViewModel
        {
            Form = row,
            Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherCourseFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit teacher course link";
        ViewData["PageTitle"] = "Teachers · Link teacher course · Edit";

        if (id != model.Form.TeacherCourseId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _teacherCourses.ExistsAsync(model.Form.TeacherId, model.Form.CourseId, id, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This teacher and course combination already exists.");
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _teacherCourses.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Teacher course link updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected teacher or course is invalid."
                : "This teacher and course combination already exists.");
            model.Lookups = await _teacherCourses.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _teacherCourses.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Teacher course link deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This link cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
