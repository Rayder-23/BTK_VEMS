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
    private readonly IEmployeeRepository _employees;

    public TeachersController(
        ITeacherRepository teachers,
        ITeacherCourseAssignmentRepository assignments,
        IEmployeeRepository employees)
    {
        _teachers = teachers;
        _assignments = assignments;
        _employees = employees;
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
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher";
        ViewData["PageTitle"] = "Teachers · Add";
        ViewData["RequireEmployeeLookup"] = true;

        var lookups = await _teachers.GetLookupsAsync(cancellationToken);
        return View(new TeacherFormPageViewModel
        {
            Lookups = lookups,
            Form = new TeacherFormModel
            {
                JoiningDate = DateTime.Today,
                IsActive = true
            }
        });
    }

    private async Task<IActionResult> ReturnCreateViewAsync(
        TeacherFormPageViewModel model,
        CancellationToken cancellationToken)
    {
        model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
        ViewData["RequireEmployeeLookup"] = true;
        if (await _employees.GetByEmployeeIdForTeacherAsync(model.Form.EmployeeCode, cancellationToken) is not null)
        {
            ViewData["EmployeeVerified"] = true;
        }

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add teacher";
        ViewData["PageTitle"] = "Teachers · Add";
        ViewData["RequireEmployeeLookup"] = true;

        await ValidateFormAsync(model.Form, null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return await ReturnCreateViewAsync(model, cancellationToken);
        }

        if (await _teachers.EmployeeCodeExistsAsync(model.Form.EmployeeCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.EmployeeCode), "Employee code already exists.");
            return await ReturnCreateViewAsync(model, cancellationToken);
        }

        if (await _teachers.EmailExistsAsync(model.Form.Email, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.Email), "Email already exists.");
            return await ReturnCreateViewAsync(model, cancellationToken);
        }

        try
        {
            var newId = await _teachers.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = $"Teacher created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            ApplyUniqueConstraintError(ex, model);
            return await ReturnCreateViewAsync(model, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyCheckConstraintError(ex, model);
            return await ReturnCreateViewAsync(model, cancellationToken);
        }
    }

    [HttpGet("lookup-employee")]
    public async Task<IActionResult> LookupEmployee(string employeeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            return BadRequest(new { found = false, message = "Employee ID is required." });
        }

        var employee = await _employees.GetByEmployeeIdForTeacherAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return Json(new { found = false, message = "No employee found with this ID." });
        }

        if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new
            {
                found = false,
                message = $"Employee status is '{employee.Status}'. Only active employees can be linked."
            });
        }

        var alreadyTeacher = await _teachers.EmployeeCodeExistsAsync(employee.EmployeeId, null, cancellationToken);
        if (alreadyTeacher)
        {
            return Json(new
            {
                found = true,
                alreadyTeacher = true,
                message = "A teacher record already exists for this employee ID."
            });
        }

        return Json(new
        {
            found = true,
            alreadyTeacher = false,
            employeeId = employee.EmployeeId,
            fullName = employee.FullName,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            email = employee.Email,
            phone = employee.Phone,
            designation = employee.Designation,
            qualification = employee.Qualification,
            specialization = employee.Specialization,
            joiningDate = employee.JoinedDate?.ToString("yyyy-MM-dd")
        });
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

        return View(new TeacherFormPageViewModel
        {
            Form = row,
            Lookups = await _teachers.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TeacherFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit teacher";
        ViewData["PageTitle"] = "Teachers · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        await ValidateFormAsync(model.Form, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _teachers.EmployeeCodeExistsAsync(model.Form.EmployeeCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.EmployeeCode), "Employee code already exists.");
            model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _teachers.EmailExistsAsync(model.Form.Email, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Form.Email), "Email already exists.");
            model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _teachers.UpdateAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
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
            model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ApplyCheckConstraintError(ex, model);
            model.Lookups = await _teachers.GetLookupsAsync(cancellationToken);
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

    private async Task ValidateFormAsync(TeacherFormModel form, int? teacherUid, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        if (teacherUid is null)
        {
            var employee = await _employees.GetByEmployeeIdForTeacherAsync(form.EmployeeCode, cancellationToken);
            if (employee is null)
            {
                ModelState.AddModelError(nameof(form.EmployeeCode), "Enter a valid employee ID from the employee register.");
                return;
            }

            if (!string.Equals(employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(form.EmployeeCode), "Only active employees can be registered as teachers.");
                return;
            }

            form.EmployeeCode = employee.EmployeeId;
        }

        var lookups = await _teachers.GetLookupsAsync(cancellationToken);

        if (form.ProgramId.HasValue && lookups.Programs.All(p => p.Id != form.ProgramId.Value))
        {
            ModelState.AddModelError(nameof(form.ProgramId), "Select a valid program.");
        }

        if (string.IsNullOrWhiteSpace(form.Designation))
        {
            form.Designation = null;
        }
        else
        {
            var matchedDesignation = lookups.Designations.FirstOrDefault(d =>
                string.Equals(d, form.Designation, StringComparison.OrdinalIgnoreCase));
            if (matchedDesignation is null)
            {
                ModelState.AddModelError(nameof(form.Designation), "Select a valid designation.");
            }
            else
            {
                form.Designation = matchedDesignation;
            }
        }
    }

    private void ApplyUniqueConstraintError(SqlException ex, TeacherFormPageViewModel model)
    {
        var message = ex.Message;

        if (message.Contains("EmployeeCode", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.EmployeeCode), "Employee code already exists.");
            return;
        }

        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.Email), "Email already exists.");
            return;
        }

        ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
    }

    private void ApplyCheckConstraintError(SqlException ex, TeacherFormPageViewModel model)
    {
        if (ex.Message.Contains("CK_Teachers_Designation", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Form.Designation), "Select a valid designation from the list.");
            return;
        }

        ModelState.AddModelError(string.Empty, "One or more values are not allowed by database rules.");
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
