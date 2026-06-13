using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/teachers")]
public sealed class TeachersController : AdminBaseController
{
    private readonly ITeacherRepository _teachers;
    private readonly ITeacherCourseAssignmentRepository _assignments;

    public TeachersController(
        ITeacherRepository teachers,
        ITeacherCourseAssignmentRepository assignments)
    {
        _teachers = teachers;
        _assignments = assignments;
    }

    [HttpGet("all")]
    [HttpGet("all/Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Teachers";
        ViewData["PageTitle"] = "Teachers";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;

        var items = await _teachers.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add teacher";
        ViewData["PageTitle"] = "Teachers · Add";
        return View(new TeacherFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher";
        ViewData["PageTitle"] = "Teachers · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _teachers.EmployeeNoExistsAsync(model.EmployeeNo, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.EmployeeNo), "Employee number already exists.");
            return View(model);
        }

        if (await _teachers.EmailExistsAsync(model.Email, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        try
        {
            var newId = await _teachers.InsertAsync(model, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = $"Teacher created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _teachers.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit teacher";
        ViewData["PageTitle"] = "Teachers · Edit";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit teacher";
        ViewData["PageTitle"] = "Teachers · Edit";

        if (id != model.TeacherId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _teachers.EmployeeNoExistsAsync(model.EmployeeNo, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.EmployeeNo), "Employee number already exists.");
            return View(model);
        }

        if (await _teachers.EmailExistsAsync(model.Email, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        try
        {
            var ok = await _teachers.UpdateAsync(model, ResolveStaffLoginUid(), cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Teacher updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _teachers.DeactivateAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Teacher deactivated." : "Teacher not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("assignments/{teacherId:int}")]
    public async Task<IActionResult> Assignments(
        int teacherId,
        bool showInactive = false,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _assignments.GetTeacherSummaryAsync(teacherId, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Teacher assignments";
        ViewData["PageTitle"] = "Teachers · Assignments";
        ViewData["ShowInactive"] = showInactive;

        return View(new TeacherAssignmentsPageViewModel
        {
            Teacher = teacher,
            Assignments = await _assignments.ListByTeacherAsync(teacherId, activeOnly: !showInactive, cancellationToken)
        });
    }

    [HttpGet("assignments/{teacherId:int}/create")]
    public async Task<IActionResult> CreateAssignment(int teacherId, CancellationToken cancellationToken)
    {
        var teacher = await _assignments.GetTeacherSummaryAsync(teacherId, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Assign class and course";
        ViewData["PageTitle"] = "Teachers · Assignments · Add";

        var lookups = await _assignments.GetLookupsAsync(cancellationToken);
        return View(new TeacherCourseAssignmentFormPageViewModel
        {
            Teacher = teacher,
            Lookups = lookups,
            Form = CreateDefaultAssignmentForm(teacherId, lookups)
        });
    }

    [HttpPost("assignments/{teacherId:int}/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(
        int teacherId,
        TeacherCourseAssignmentFormPageViewModel model,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Assign class and course";
        ViewData["PageTitle"] = "Teachers · Assignments · Add";

        var teacher = await _assignments.GetTeacherSummaryAsync(teacherId, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        model.Teacher = teacher;
        model.Form.TeacherId = teacherId;

        await ValidateAssignmentFormAsync(model.Form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _assignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _assignments.AssignmentExistsAsync(model.Form, null, cancellationToken))
        {
            ModelState.AddModelError(string.Empty,
                "This teacher already has the same class, course, semester, year, and day assignment.");
            model.Lookups = await _assignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var newId = await _assignments.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Assignment created (id {newId}).";
        return RedirectToAction(nameof(Assignments), new { teacherId });
    }

    [HttpGet("assignments/edit/{assignmentId:int}")]
    public async Task<IActionResult> EditAssignment(int assignmentId, CancellationToken cancellationToken)
    {
        var form = await _assignments.GetAsync(assignmentId, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        var teacher = await _assignments.GetTeacherSummaryAsync(form.TeacherId, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit assignment";
        ViewData["PageTitle"] = "Teachers · Assignments · Edit";

        return View(new TeacherCourseAssignmentFormPageViewModel
        {
            Teacher = teacher,
            Form = form,
            Lookups = await _assignments.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("assignments/edit/{assignmentId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssignment(
        int assignmentId,
        TeacherCourseAssignmentFormPageViewModel model,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit assignment";
        ViewData["PageTitle"] = "Teachers · Assignments · Edit";

        if (assignmentId != model.Form.Uid)
        {
            return NotFound();
        }

        var teacher = await _assignments.GetTeacherSummaryAsync(model.Form.TeacherId, cancellationToken);
        if (teacher is null)
        {
            return NotFound();
        }

        model.Teacher = teacher;

        await ValidateAssignmentFormAsync(model.Form, assignmentId, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _assignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _assignments.AssignmentExistsAsync(model.Form, assignmentId, cancellationToken))
        {
            ModelState.AddModelError(string.Empty,
                "This teacher already has the same class, course, semester, year, and day assignment.");
            model.Lookups = await _assignments.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var ok = await _assignments.UpdateAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Assignment updated.";
        return RedirectToAction(nameof(Assignments), new { teacherId = model.Form.TeacherId });
    }

    [HttpPost("assignments/delete/{assignmentId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssignment(int assignmentId, CancellationToken cancellationToken)
    {
        var form = await _assignments.GetAsync(assignmentId, cancellationToken);
        if (form is null)
        {
            TempData["StatusMessage"] = "Assignment not found.";
            return RedirectToAction(nameof(Index));
        }

        var ok = await _assignments.DeactivateAsync(assignmentId, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Assignment deactivated." : "Assignment not found.";
        return RedirectToAction(nameof(Assignments), new { teacherId = form.TeacherId });
    }

    private void ApplyUniqueConstraintError(SqlException ex, TeacherFormModel model)
    {
        var message = ex.Message;

        if (message.Contains("EmployeeNo", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.EmployeeNo), "Employee number already exists.");
            return;
        }

        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return;
        }

        ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
    }

    private async Task ValidateAssignmentFormAsync(
        TeacherCourseAssignmentFormModel form,
        int? assignmentUid,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var lookups = await _assignments.GetLookupsAsync(cancellationToken);

        if (lookups.Classes.All(c => c.Id != form.ClassId))
        {
            ModelState.AddModelError(nameof(form.ClassId), "Select a valid class.");
        }

        if (lookups.Courses.All(c => c.Id != form.CourseId))
        {
            ModelState.AddModelError(nameof(form.CourseId), "Select a valid course.");
        }

        var matchedSemester = lookups.Semesters.FirstOrDefault(s =>
            string.Equals(s, form.Semester, StringComparison.OrdinalIgnoreCase));
        if (matchedSemester is null)
        {
            ModelState.AddModelError(nameof(form.Semester), "Select a valid semester.");
        }
        else
        {
            form.Semester = matchedSemester;
        }

        if (!string.IsNullOrWhiteSpace(form.DayOfWeek))
        {
            var matchedDay = lookups.DaysOfWeek.FirstOrDefault(d =>
                string.Equals(d, form.DayOfWeek, StringComparison.OrdinalIgnoreCase));
            if (matchedDay is null)
            {
                ModelState.AddModelError(nameof(form.DayOfWeek), "Select a valid day of week.");
            }
            else
            {
                form.DayOfWeek = matchedDay;
            }
        }
        else
        {
            form.DayOfWeek = null;
        }

        if (form.StartTime.HasValue && form.EndTime.HasValue && form.EndTime <= form.StartTime)
        {
            ModelState.AddModelError(nameof(form.EndTime), "End time must be after start time.");
        }
    }

    private static TeacherCourseAssignmentFormModel CreateDefaultAssignmentForm(
        int teacherId,
        TeacherCourseAssignmentLookups lookups) => new()
    {
        TeacherId = teacherId,
        ClassId = lookups.Classes.FirstOrDefault()?.Id ?? 0,
        CourseId = lookups.Courses.FirstOrDefault()?.Id ?? 0,
        Semester = lookups.Semesters.FirstOrDefault() ?? string.Empty,
        AcademicYear = (short)DateTime.UtcNow.Year,
        IsActive = true
    };

    private int ResolveActorId() => ResolveStaffLoginUid() ?? 1;
}
