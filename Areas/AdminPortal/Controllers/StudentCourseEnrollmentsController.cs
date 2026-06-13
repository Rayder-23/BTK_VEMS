using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/course-enrollments")]
public sealed class StudentCourseEnrollmentsController : StudentMgmtBaseController
{
    private readonly IStudentCourseEnrollmentRepository _enrollments;

    public StudentCourseEnrollmentsController(IStudentCourseEnrollmentRepository enrollments)
    {
        _enrollments = enrollments;
    }

    protected override string ModuleKey => "CourseEnrollments";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Course enrollments";
        ViewData["PageTitle"] = "Course Enrollments · All";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _enrollments.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? studentId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Enroll student";
        ViewData["PageTitle"] = "Course Enrollments · Enroll";

        var form = new StudentCourseEnrollmentFormModel
        {
            IsActive = true,
            Status = "Active"
        };
        if (studentId is > 0)
        {
            form.StudentId = studentId.Value;
        }

        return View(new StudentCourseEnrollmentFormPageViewModel
        {
            Form = form,
            Lookups = await _enrollments.GetLookupsAsync(form.StudentId > 0 ? form.StudentId : null, cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCourseEnrollmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Enroll student";
        ViewData["PageTitle"] = "Course Enrollments · Enroll";

        await ValidateFormAsync(model.Form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }

        if (await _enrollments.ExistsAsync(model.Form.StudentId, model.Form.ClassSectionCourseId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassSectionCourseId), "This student is already enrolled in the selected class course.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _enrollments.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Student course enrollment created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassSectionCourseId), "This student is already enrolled in the selected class course.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(string.Empty, "Enrollment, student, or class course reference is invalid.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
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

        ViewData["Title"] = "Edit enrollment";
        ViewData["PageTitle"] = "Course Enrollments · Edit";

        return View(new StudentCourseEnrollmentFormPageViewModel
        {
            Form = row,
            Lookups = await _enrollments.GetLookupsAsync(row.StudentId, cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentCourseEnrollmentFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit enrollment";
        ViewData["PageTitle"] = "Course Enrollments · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }

        if (await _enrollments.ExistsAsync(model.Form.StudentId, model.Form.ClassSectionCourseId, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.ClassSectionCourseId), "This student is already enrolled in the selected class course.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _enrollments.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Student course enrollment updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ModelState.AddModelError(nameof(model.Form.ClassSectionCourseId), "This student is already enrolled in the selected class course.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(string.Empty, "Enrollment, student, or class course reference is invalid.");
            model.Lookups = await _enrollments.GetLookupsAsync(model.Form.StudentId, cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _enrollments.DeactivateAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Enrollment deactivated." : "Record not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(int studentId, CancellationToken cancellationToken)
    {
        if (studentId <= 0)
        {
            return BadRequest();
        }

        var lookups = await _enrollments.GetLookupsAsync(studentId, cancellationToken);
        return Json(lookups.ProgramEnrollments.Select(e => new { id = e.Id, name = e.Name }));
    }

    private async Task ValidateFormAsync(StudentCourseEnrollmentFormModel form, int? uid, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _enrollments.GetLookupsAsync(form.StudentId, cancellationToken);

        if (lookups.Students.All(s => s.Id != form.StudentId))
        {
            ModelState.AddModelError(nameof(form.StudentId), "Select a valid student.");
        }

        if (lookups.ClassSectionCourses.All(c => c.Id != form.ClassSectionCourseId))
        {
            ModelState.AddModelError(nameof(form.ClassSectionCourseId), "Select a valid class course.");
        }

        if (!await _enrollments.EnrollmentBelongsToStudentAsync(form.EnrollmentId, form.StudentId, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.EnrollmentId), "Select a program enrollment that belongs to this student.");
        }

        var matchedStatus = lookups.Statuses.FirstOrDefault(s =>
            string.Equals(s, form.Status, StringComparison.OrdinalIgnoreCase));
        if (matchedStatus is null)
        {
            ModelState.AddModelError(nameof(form.Status), "Select a valid status.");
        }
        else if (!StudentCourseEnrollmentRepository.AllowedStatuses.Any(a =>
                     string.Equals(a, matchedStatus, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(form.Status), "Status is not allowed by database rules.");
        }
        else
        {
            form.Status = StudentCourseEnrollmentRepository.AllowedStatuses.First(a =>
                string.Equals(a, matchedStatus, StringComparison.OrdinalIgnoreCase));
        }
    }
}
