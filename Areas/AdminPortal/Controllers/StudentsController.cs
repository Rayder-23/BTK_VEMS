using System.ComponentModel.DataAnnotations;
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
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Student";
        ViewData["PageTitle"] = "Students · Add";
        var lookups = await _students.GetLookupsAsync(cancellationToken);
        return View(new StudentFormViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Student";
        ViewData["PageTitle"] = "Students · Add";

        ValidateStudentForm(model.Form);
        if (!ModelState.IsValid)
        {
            model.Lookups = await _students.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _students.InsertAsync(model.Form, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = $"Student created (id {newId}) with program enrollment.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            if (ex.Message.Contains("RollNo", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("UQ_Enrollments", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Form.RollNo", "Roll number already exists for this program, year, and semester.");
            }
            else if (ex.Message.Contains("RegistrationNo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Form.RegistrationNo", "Registration number already exists.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "A record with the same unique value already exists.");
            }

            model.Lookups = await _students.GetLookupsAsync(cancellationToken);
            return View(model);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ModelState.AddModelError(string.Empty, "Student or program enrollment could not be saved because a related reference is invalid.");
            model.Lookups = await _students.GetLookupsAsync(cancellationToken);
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

        return View(new StudentFormViewModel
        {
            Form = row,
            Lookups = await _students.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StudentFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Student";
        ViewData["PageTitle"] = "Students · Edit";

        if (model.Form.Uid <= 0)
        {
            return NotFound();
        }

        ValidateStudentForm(model.Form);
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

    private void ValidateStudentForm(StudentFormModel form)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);
        foreach (var result in results)
        {
            var members = result.MemberNames.Any()
                ? result.MemberNames.Select(m => $"Form.{m}")
                : ["Form"];
            foreach (var key in members)
            {
                ModelState.AddModelError(key, result.ErrorMessage ?? "Invalid value.");
            }
        }
    }

    private static StudentFormModel CreateDefaultForm(StudentLookups lookups)
    {
        return new StudentFormModel
        {
            AdmissionYear = (short)DateTime.Today.Year,
            AdmissionDate = DateTime.Today,
            DateOfBirth = DateTime.Today.AddYears(-18),
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
