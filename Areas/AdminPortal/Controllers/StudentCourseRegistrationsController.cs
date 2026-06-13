using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/settings/student-course-registrations")]
public sealed class StudentCourseRegistrationsController : AdminBaseController
{
    private readonly IStudentCourseRegistrationRepository _registrations;

    public StudentCourseRegistrationsController(IStudentCourseRegistrationRepository registrations)
    {
        _registrations = registrations;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Uni student course registrations";
        ViewData["PageTitle"] = "Settings · Uni student course registrations";
        ViewData["Search"] = search;

        var items = await _registrations.ListAsync(search, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student course registration";
        ViewData["PageTitle"] = "Settings · Add student course registration";

        return View(new StudentCourseRegistrationFormPageViewModel
        {
            Lookups = await _registrations.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCourseRegistrationFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add student course registration";
        ViewData["PageTitle"] = "Settings · Add student course registration";

        if (!ModelState.IsValid)
        {
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _registrations.ExistsAsync(model.Form.StudentId, model.Form.CourseSectionId, null, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This student is already registered for the selected course section.");
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var newId = await _registrations.InsertAsync(model.Form, cancellationToken);
            TempData["StatusMessage"] = $"Student course registration created (id {newId}).";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected student or course section is invalid."
                : "This student is already registered for the selected course section.");
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _registrations.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit student course registration";
        ViewData["PageTitle"] = "Settings · Edit student course registration";

        return View(new StudentCourseRegistrationFormPageViewModel
        {
            Form = row,
            Lookups = await _registrations.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentCourseRegistrationFormPageViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit student course registration";
        ViewData["PageTitle"] = "Settings · Edit student course registration";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _registrations.ExistsAsync(model.Form.StudentId, model.Form.CourseSectionId, id, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "This student is already registered for the selected course section.");
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var ok = await _registrations.UpdateAsync(model.Form, cancellationToken);
            if (!ok)
            {
                return NotFound();
            }

            TempData["StatusMessage"] = "Student course registration updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601 or 547)
        {
            ModelState.AddModelError(string.Empty, ex.Number == 547
                ? "Selected student or course section is invalid."
                : "This student is already registered for the selected course section.");
            model.Lookups = await _registrations.GetLookupsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _registrations.DeleteAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok ? "Student course registration deleted." : "Record not found.";
        return RedirectToAction(nameof(Index));
    }
}
