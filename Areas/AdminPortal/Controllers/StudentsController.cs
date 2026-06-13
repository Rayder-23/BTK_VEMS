using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/students/students")]
public sealed class StudentsController : StudentMgmtBaseController
{
    private readonly IStudentRepository _students;

    public StudentsController(IStudentRepository students)
    {
        _students = students;
    }

    protected override string ModuleKey => "Students";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "All Students";
        ViewData["PageTitle"] = "Students · All Students";
        var items = await _students.ListAsync(cancellationToken);
        var active = items.Where(s => s.IsActive).ToList();
        return View("Index", active);
    }

    [HttpGet("previous")]
    public async Task<IActionResult> Previous(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Previous Students";
        ViewData["PageTitle"] = "Students · Previous Students";
        var items = await _students.ListAsync(cancellationToken);
        var inactive = items.Where(s => !s.IsActive).ToList();
        return View("Index", inactive);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Student";
        ViewData["PageTitle"] = "Students · Add";
        return View(new StudentFormModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Student";
        ViewData["PageTitle"] = "Students · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var newId = await _students.InsertAsync(model, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = $"Student created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            if (ex.Message.Contains("RegistrationNo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.RegistrationNo), "Registration number already exists.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
            }

            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Student";
        ViewData["PageTitle"] = "Students · Edit";

        var row = await _students.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Student";
        ViewData["PageTitle"] = "Students · Edit";

        if (id != model.StudentId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var ok = await _students.UpdateAsync(model, ResolveActorId(), cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Student updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            if (ex.Message.Contains("RegistrationNo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.RegistrationNo), "Registration number already exists.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
            }

            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _students.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok
                ? "Student deleted."
                : "Student could not be deleted (record not found).";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "Student could not be deleted because other records (login, enrollments, challans, etc.) still reference this student.";
        }

        return RedirectToAction(nameof(Index));
    }

    private int ResolveActorId() => 1;
}
