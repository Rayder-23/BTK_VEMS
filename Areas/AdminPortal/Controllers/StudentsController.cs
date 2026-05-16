using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class StudentsController : AdminBaseController
{
    private readonly IStudentRepository _students;

    public StudentsController(IStudentRepository students)
    {
        _students = students;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Students";
        ViewData["PageTitle"] = "Students";
        var items = await _students.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add student";
        ViewData["PageTitle"] = "Students · Add";
        return View(new StudentFormModel
        {
            EnrolledDate = DateTime.Today,
            Status = "Active"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student";
        ViewData["PageTitle"] = "Students · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newId = await _students.InsertAsync(model, cancellationToken);
        TempData["StatusMessage"] = $"Student created (internal id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit student";
        ViewData["PageTitle"] = "Students · Edit";

        var row = await _students.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit student";
        ViewData["PageTitle"] = "Students · Edit";

        if (model.Uid <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ok = await _students.UpdateAsync(model, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Student updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
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
            TempData["ErrorMessage"] = "Student could not be deleted because other records (fees, challans, etc.) still reference this student.";
        }

        return RedirectToAction(nameof(Index));
    }
}
