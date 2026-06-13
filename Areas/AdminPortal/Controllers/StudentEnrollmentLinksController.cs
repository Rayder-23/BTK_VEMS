using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/student-enrollments")]
public sealed class StudentEnrollmentLinksController : AdminBaseController
{
    private readonly IStudentEnrollmentLinkRepository _studentEnrollments;

    public StudentEnrollmentLinksController(IStudentEnrollmentLinkRepository studentEnrollments)
    {
        _studentEnrollments = studentEnrollments;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Link student enrollments";
        ViewData["PageTitle"] = "Students · Link student enrollments";
        ViewData["Search"] = search;

        var items = await _studentEnrollments.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student enrollment";
        ViewData["PageTitle"] = "Students · Link student enrollments · Add";

        return View(new StudentEnrollmentLinkFormPageViewModel
        {
            Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentEnrollmentLinkFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student enrollment";
        ViewData["PageTitle"] = "Students · Link student enrollments · Add";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _studentEnrollments.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Student enrollment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This student enrollment already exists.");
            model.Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _studentEnrollments.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit student enrollment";
        ViewData["PageTitle"] = "Students · Link student enrollments · Edit";

        return View(new StudentEnrollmentLinkFormPageViewModel
        {
            Form = row,
            Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentEnrollmentLinkFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit student enrollment";
        ViewData["PageTitle"] = "Students · Link student enrollments · Edit";

        if (id != model.Form.StudentEnrollmentId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _studentEnrollments.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Student enrollment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This student enrollment already exists.");
            model.Lookups = await _studentEnrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _studentEnrollments.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Student enrollment deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This enrollment cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
