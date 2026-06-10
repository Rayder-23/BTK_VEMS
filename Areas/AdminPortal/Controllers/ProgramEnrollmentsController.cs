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
            AcademicYear = (short)DateTime.Today.Year,
            EnrollmentDate = DateTime.Today,
            EnrollmentStatus = "Active",
            FeeStatus = "Pending",
            GradeOrSemester = 1,
            SectionId = 1
        };

        if (studentId is > 0)
        {
            form.StudentId = studentId.Value;
        }

        return View(new ProgramEnrollmentFormPageViewModel
        {
            Form = form,
            Lookups = await _enrollments.GetLookupsAsync(form.ProgramId > 0 ? form.ProgramId : null, cancellationToken)
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
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _enrollments.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = $"Program enrollment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyCheckConstraintError(ex, model);
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
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
            Lookups = await _enrollments.GetLookupsAsync(row.ProgramId, cancellationToken)
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
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _enrollments.UpdateAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Program enrollment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyCheckConstraintError(ex, model);
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.ProgramId, cancellationToken);
            return View(model);
        }
    }

    [HttpPost("withdraw/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken)
    {
        var ok = await _enrollments.WithdrawAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Program enrollment withdrawn." : "Record not found or already withdrawn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(int programId, CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest();
        }

        var lookups = await _enrollments.GetLookupsAsync(programId, cancellationToken);
        return Json(lookups.Sections.Select(s => new { id = s.Id, name = s.Name }));
    }

    private async Task ValidateFormAsync(ProgramEnrollmentFormModel form, int? uid, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _enrollments.GetLookupsAsync(form.ProgramId, cancellationToken);

        if (lookups.Students.All(s => s.Id != form.StudentId))
        {
            ModelState.AddModelError(nameof(form.StudentId), "Select a valid student.");
        }

        if (lookups.Programs.All(p => p.Id != form.ProgramId))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        if (lookups.Sections.All(s => s.Id != form.SectionId))
        {
            ModelState.AddModelError(nameof(form.SectionId), "Select a valid section.");
        }

        var enrollmentStatus = lookups.EnrollmentStatuses.FirstOrDefault(s =>
            string.Equals(s, form.EnrollmentStatus, StringComparison.OrdinalIgnoreCase));
        if (enrollmentStatus is null)
        {
            ModelState.AddModelError(nameof(form.EnrollmentStatus), "Select a valid enrollment status.");
        }
        else
        {
            form.EnrollmentStatus = ProgramEnrollmentRepository.AllowedEnrollmentStatuses.First(a =>
                string.Equals(a, enrollmentStatus, StringComparison.OrdinalIgnoreCase));
        }

        var feeStatus = lookups.FeeStatuses.FirstOrDefault(s =>
            string.Equals(s, form.FeeStatus, StringComparison.OrdinalIgnoreCase));
        if (feeStatus is null)
        {
            ModelState.AddModelError(nameof(form.FeeStatus), "Select a valid fee status.");
        }
        else
        {
            form.FeeStatus = ProgramEnrollmentRepository.AllowedFeeStatuses.First(a =>
                string.Equals(a, feeStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (await _enrollments.ExistsForPeriodAsync(
                form.StudentId, form.ProgramId, form.AcademicYear, form.GradeOrSemester, uid, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(form.GradeOrSemester),
                "This student already has an enrollment for the same program, year, and semester.");
        }

        if (await _enrollments.RollNoExistsAsync(
                form.ProgramId, form.AcademicYear, form.GradeOrSemester, form.RollNo, uid, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.RollNo), "Roll number already exists for this program cohort.");
        }
    }

    private void ApplyUniqueConstraintError(SqlException ex, ProgramEnrollmentFormPageViewModel model)
    {
        if (ex.Message.Contains("UQ_Enrollments_RollNo", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("RollNo", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.RollNo), "Roll number already exists for this program cohort.");
            return;
        }

        if (ex.Message.Contains("UQ_Enrollments_Period", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.Form.GradeOrSemester),
                "This student already has an enrollment for the same program, year, and semester.");
            return;
        }

        ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
    }

    private void ApplyCheckConstraintError(SqlException ex, ProgramEnrollmentFormPageViewModel model)
    {
        var message = ex.Message;

        if (message.Contains("CK_Enrollments_Status", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.EnrollmentStatus), "Select a valid enrollment status.");
            return;
        }

        if (message.Contains("CK_Enrollments_Fee", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.FeeStatus), "Select a valid fee status.");
            return;
        }

        ModelState.AddModelError(string.Empty, "Student, program, or section reference is invalid.");
    }

    private int ResolveActorId() => ResolveStaffLoginUid() ?? 1;
}
