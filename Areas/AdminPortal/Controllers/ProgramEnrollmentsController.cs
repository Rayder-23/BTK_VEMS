using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/program-enrollments")]
public sealed class ProgramEnrollmentsController : StudentMgmtBaseController
{
    private readonly IProgramEnrollmentRepository _enrollments;

    public ProgramEnrollmentsController(IProgramEnrollmentRepository enrollments)
    {
        _enrollments = enrollments;
    }

    protected override string ModuleKey => "ProgramEnrollments";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Program enrollments";
        ViewData["PageTitle"] = "Program Enrollments · All";
        ViewData["Search"] = search;

        var items = await _enrollments.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? studentId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add program enrollment";
        ViewData["PageTitle"] = "Program Enrollments · Add";

        var form = new ProgramEnrollmentFormModel
        {
            EnrollmentDate = DateTime.Today
        };

        if (studentId is > 0)
        {
            form.StudentId = studentId.Value;
        }

        return View(new ProgramEnrollmentFormPageViewModel
        {
            Form = form,
            Lookups = await _enrollments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramEnrollmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add program enrollment";
        ViewData["PageTitle"] = "Program Enrollments · Add";

        await ValidateFormAsync(model.Form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _enrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _enrollments.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Program enrollment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This student already has an enrollment for the same program and academic year.");
            model.Lookups = await _enrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _enrollments.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit program enrollment";
        ViewData["PageTitle"] = "Program Enrollments · Edit";

        return View(new ProgramEnrollmentFormPageViewModel
        {
            Form = row,
            Lookups = await _enrollments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProgramEnrollmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit program enrollment";
        ViewData["PageTitle"] = "Program Enrollments · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _enrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _enrollments.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Program enrollment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "One or more selected values are invalid."
                : "This student already has an enrollment for the same program and academic year.");
            model.Lookups = await _enrollments.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _enrollments.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Program enrollment deleted." : "Record not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "This enrollment cannot be deleted because other records still reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(ProgramEnrollmentFormModel form, int? uid, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _enrollments.GetLookupsAsync(cancellationToken);

        if (lookups.AcademicYears.All(y => y.Id != form.AcademicYearId))
        {
            ModelState.AddModelError(nameof(form.AcademicYearId), "Select a valid academic year.");
        }

        if (lookups.Students.All(s => s.Id != form.StudentId))
        {
            ModelState.AddModelError(nameof(form.StudentId), "Select a valid student.");
        }

        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        if (form.ClassSectionId is > 0 && lookups.ClassSections.All(c => c.Id != form.ClassSectionId))
        {
            ModelState.AddModelError(nameof(form.ClassSectionId), "Select a valid class section.");
        }

        if (await _enrollments.ExistsAsync(form.StudentId, form.ProgramId, form.AcademicYearId, uid, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(form.AcademicYearId),
                "This student already has an enrollment for the same program and academic year.");
        }
    }
}
