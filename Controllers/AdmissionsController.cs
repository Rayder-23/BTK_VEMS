using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Models;
using VEMS.Services;

namespace VEMS.Controllers;

[Route("admissions")]
public sealed class AdmissionsController : Controller
{
    private readonly IStudentApplicationRepository _applications;
    private readonly IStudentApplicationPageFactory _pageFactory;

    public AdmissionsController(
        IStudentApplicationRepository applications,
        IStudentApplicationPageFactory pageFactory)
    {
        _applications = applications;
        _pageFactory = pageFactory;
    }

    [HttpGet("apply")]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Apply for admission";
        var page = await _pageFactory.CreateAsync(cancellationToken: cancellationToken);
        page.SuccessMessage = TempData["ApplicationSuccessMessage"] as string;
        page.ReferenceNo = TempData["ApplicationReferenceNo"] as string;
        return View("Apply", page);
    }

    [HttpPost("apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        [Bind(Prefix = "Form")] StudentApplicationFormModel model,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Apply for admission";

        if (!model.DateOfBirth.HasValue)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth is required.");
        }

        await ValidateProgramSelectionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            var page = await _pageFactory.CreateAsync(model, cancellationToken);
            return View("Apply", page);
        }

        try
        {
            var applicationNo = await _applications.InsertAsync(model, cancellationToken);
            TempData["ApplicationSuccessMessage"] = "Your application has been submitted successfully.";
            TempData["ApplicationReferenceNo"] = applicationNo;
            return RedirectToAction(nameof(Apply));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (SqlException)
        {
            ModelState.AddModelError(string.Empty, "We could not save your application. Please try again or contact the admissions office.");
        }

        var invalidPage = await _pageFactory.CreateAsync(model, cancellationToken);
        return View("Apply", invalidPage);
    }

    private async Task ValidateProgramSelectionAsync(
        StudentApplicationFormModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.ProgramName) && string.IsNullOrWhiteSpace(model.ProgramCode))
        {
            ModelState.AddModelError(nameof(model.ProgramCode), "Program is required.");
            return;
        }

        var programs = await _applications.GetActiveProgramsAsync(cancellationToken);
        var match = programs.FirstOrDefault(p =>
            (!string.IsNullOrWhiteSpace(model.ProgramName)
                && string.Equals(p.ProgramName, model.ProgramName.Trim(), StringComparison.OrdinalIgnoreCase))
            || string.Equals(p.ProgramCode, model.ProgramCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            ModelState.AddModelError(nameof(model.ProgramCode), "The selected program is not available.");
            return;
        }

        model.ProgramName = match.ProgramName;
        model.InstTypeCode = StudentApplicationFieldDefaults.ResolveInstTypeCode(match.InstTypeCode);
        model.ProgramCode = match.ProgramCode;
    }
}
