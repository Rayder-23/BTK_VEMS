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
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student";
        ViewData["PageTitle"] = "Students · Add";
        var lookups = await _students.GetLookupsAsync(cancellationToken);
        return View(new StudentFormViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student";
        ViewData["PageTitle"] = "Students · Add";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _students.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var newId = await _students.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Student created (id {newId}).";
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

        return View(new StudentFormViewModel
        {
            Form = row,
            Lookups = await _students.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit student";
        ViewData["PageTitle"] = "Students · Edit";

        if (model.Form.Uid <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _students.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var ok = await _students.UpdateAsync(model.Form, ResolveActorId(), cancellationToken);
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
            TempData["ErrorMessage"] = "Student could not be deleted because other records (login, enrollments, challans, etc.) still reference this student.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static StudentFormModel CreateDefaultForm(StudentLookups lookups)
    {
        return new StudentFormModel
        {
            AdmissionYear = (short)DateTime.Today.Year,
            AdmissionDate = DateTime.Today,
            Gender = "M",
            Nationality = "Pakistani",
            IsActive = true,
            CountryId = lookups.Countries.FirstOrDefault()?.Id ?? 1,
            ProvinceId = lookups.Provinces.FirstOrDefault()?.Id ?? 1,
            CityId = lookups.Cities.FirstOrDefault()?.Id ?? 1,
            ProgramId = lookups.Programs.FirstOrDefault()?.Id ?? 1
        };
    }

    private int ResolveActorId() => 1;
}
