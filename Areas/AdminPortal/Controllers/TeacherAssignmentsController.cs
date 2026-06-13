using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/teachers/link-teacher-assignment")]
public sealed class TeacherAssignmentsController : AdminBaseController
{
    private readonly ITeacherAssignmentRepository _teacherAssignments;

    public TeacherAssignmentsController(ITeacherAssignmentRepository teacherAssignments)
    {
        _teacherAssignments = teacherAssignments;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link teacher assignment";
        ViewData["PageTitle"] = "Teachers · Link teacher assignment";
        ViewData["Search"] = search;

        var items = await _teacherAssignments.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher assignment";
        ViewData["PageTitle"] = "Teachers · Link teacher assignment · Add";

        return View(new TeacherAssignmentFormPageViewModel
        {
            Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherAssignmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher assignment";
        ViewData["PageTitle"] = "Teachers · Link teacher assignment · Add";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _teacherAssignments.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Teacher assignment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This teacher assignment already exists.");
            model.Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _teacherAssignments.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit teacher assignment";
        ViewData["PageTitle"] = "Teachers · Link teacher assignment · Edit";

        return View(new TeacherAssignmentFormPageViewModel
        {
            Form = row,
            Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherAssignmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit teacher assignment";
        ViewData["PageTitle"] = "Teachers · Link teacher assignment · Edit";

        if (id != model.Form.TeacherAssignmentId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _teacherAssignments.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Teacher assignment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This teacher assignment already exists.");
            model.Lookups = await _teacherAssignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _teacherAssignments.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Teacher assignment deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This assignment cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
