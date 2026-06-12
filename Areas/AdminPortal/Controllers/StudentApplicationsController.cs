using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models.Admissions;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services.Admissions;
using VEMS.Areas.AdminPortal.Services.Fee;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/admissions/applications")]
public sealed class StudentApplicationsController : AdminBaseController
{
    private readonly IStudentApplicationAdminRepository _applications;
    private readonly IFeeChallanRepository _challans;

    public StudentApplicationsController(
        IStudentApplicationAdminRepository applications,
        IFeeChallanRepository challans)
    {
        _applications = applications;
        _challans = challans;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Applications";
        ViewData["PageTitle"] = "Admissions · Applications";
        ViewData["Search"] = search;
        ViewData["Status"] = status;
        var lookups = await _applications.GetLookupsAsync(cancellationToken);
        ViewData["StatusOptions"] = lookups.ApplicationStatuses;
        var items = await _applications.ListAsync(search, status, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Application";
        ViewData["PageTitle"] = "Admissions · Add";
        var lookups = await _applications.GetLookupsAsync(cancellationToken);
        var appNo = await _applications.GenerateApplicationNoAsync(cancellationToken);
        return View(new StudentApplicationFormViewModel
        {
            Lookups = lookups,
            Form = CreateDefaultForm(lookups, appNo)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentApplicationFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Application";
        ViewData["PageTitle"] = "Admissions · Add";
        ValidateForm(model.Form);

        if (!ModelState.IsValid || !await ValidateProgramAsync(model.Form, cancellationToken))
        {
            model.Lookups = await _applications.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _applications.ApplicationNoExistsAsync(model.Form.ApplicationNo, null, cancellationToken))
        {
            ModelState.AddModelError("Form.ApplicationNo", "Application number already exists.");
            model.Lookups = await _applications.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var id = await _applications.InsertAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = $"Application created (id {id}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _applications.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Application";
        ViewData["PageTitle"] = "Admissions · Edit";
        return View(new StudentApplicationFormViewModel
        {
            Form = row,
            Lookups = await _applications.GetLookupsAsync(cancellationToken)
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentApplicationFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Application";
        ViewData["PageTitle"] = "Admissions · Edit";

        if (id != model.Form.Uid)
        {
            return NotFound();
        }

        var existing = await _applications.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (string.Equals(
                existing.ApplicationStatus,
                StudentApplicationAdminRepository.ConvertedApplicationStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Converted applications cannot be edited.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        ValidateForm(model.Form);
        if (!ModelState.IsValid || !await ValidateProgramAsync(model.Form, cancellationToken))
        {
            model.Lookups = await _applications.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        if (await _applications.ApplicationNoExistsAsync(model.Form.ApplicationNo, id, cancellationToken))
        {
            ModelState.AddModelError("Form.ApplicationNo", "Application number already exists.");
            model.Lookups = await _applications.GetLookupsAsync(cancellationToken);
            return View(model);
        }

        var ok = await _applications.UpdateAsync(model.Form, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Application updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("challan/{id:int}")]
    public async Task<IActionResult> CreateChallan(int id, CancellationToken cancellationToken)
    {
        var model = await _applications.GetApplicationChallanPrefillAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Create Challan";
        ViewData["PageTitle"] = "Admissions · Create Challan";
        return View(model);
    }

    [HttpPost("challan/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChallan(
        int id,
        ApplicationChallanGenerateFormModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.ApplicationUid)
        {
            return BadRequest();
        }

        var prefill = await _applications.GetApplicationChallanPrefillAsync(id, cancellationToken);
        if (prefill is null)
        {
            return NotFound();
        }

        if (!prefill.FeeStructureFound || prefill.StructureId <= 0)
        {
            ModelState.AddModelError(string.Empty, "No admission fee structure found for this application's program and year.");
        }

        if (model.DueDate < model.IssueDate)
        {
            ModelState.AddModelError(nameof(model.DueDate), "Due date cannot be before issue date.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Create Challan";
            ViewData["PageTitle"] = "Admissions · Create Challan";
            model.ApplicationNo = prefill.ApplicationNo;
            model.ApplicantName = prefill.ApplicantName;
            model.ProgramName = prefill.ProgramName;
            model.DesiredYear = prefill.DesiredYear;
            model.StructureId = prefill.StructureId;
            model.StructureLabel = prefill.StructureLabel;
            model.AdmissionFeeAmount = prefill.AdmissionFeeAmount;
            model.FeeStructureFound = prefill.FeeStructureFound;
            model.FeeMessage = prefill.FeeMessage;
            return View(model);
        }

        try
        {
            var challanId = await _challans.GenerateChallanAsync(new ChallanGenerateFormModel
            {
                ApplicationUid = id,
                StructureId = prefill.StructureId,
                IssueDate = model.IssueDate,
                DueDate = model.DueDate,
                Remarks = model.Remarks,
                DiscountAmount = model.DiscountAmount
            }, ResolveStaffLoginUid() ?? 1, cancellationToken);

            TempData["StatusMessage"] = $"Challan created for application {prefill.ApplicationNo}.";
            return Redirect($"/adminportal/fee/challans/details/{challanId}");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "Create Challan";
            ViewData["PageTitle"] = "Admissions · Create Challan";
            model.ApplicationNo = prefill.ApplicationNo;
            model.ApplicantName = prefill.ApplicantName;
            model.ProgramName = prefill.ProgramName;
            model.DesiredYear = prefill.DesiredYear;
            model.StructureId = prefill.StructureId;
            model.StructureLabel = prefill.StructureLabel;
            model.AdmissionFeeAmount = prefill.AdmissionFeeAmount;
            model.FeeStructureFound = prefill.FeeStructureFound;
            model.FeeMessage = prefill.FeeMessage;
            return View(model);
        }
    }

    [HttpGet("pick-admission-fee")]
    public async Task<IActionResult> PickAdmissionFee(
        string programCode,
        short academicYear,
        CancellationToken cancellationToken)
    {
        var result = await _applications.GetAdmissionFeeAmountAsync(programCode, academicYear, cancellationToken);
        return Json(result);
    }

    [HttpPost("convert-as-student/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertAsStudent(int id, CancellationToken cancellationToken)
    {
        try
        {
            var createdBy = ResolveStaffLoginUid() ?? 1;
            var studentId = await _applications.ConvertToStudentAsync(id, createdBy, cancellationToken);
            TempData["StatusMessage"] = $"Applicant converted to student (id {studentId}). Application status set to Converted As Student.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (SqlException ex)
        {
            TempData["ErrorMessage"] = ex.Number == 547
                ? VEMS.Services.SqlForeignKeyViolationFormatter.Describe(ex)
                : $"Could not convert applicant to student: {ex.Message}";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _applications.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok ? "Application deleted." : "Application not found.";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "Application could not be deleted because other records reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateProgramAsync(StudentApplicationFormModel form, CancellationToken cancellationToken)
    {
        var lookups = await _applications.GetLookupsAsync(cancellationToken);
        var match = lookups.Programs.FirstOrDefault(p =>
            (!string.IsNullOrWhiteSpace(form.ProgramName)
                && string.Equals(p.ProgramName, form.ProgramName.Trim(), StringComparison.OrdinalIgnoreCase))
            || string.Equals(p.ProgramCode, form.ProgramCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            ModelState.AddModelError("Form.ProgramCode", "Select a valid program.");
            return false;
        }

        form.ProgramName = match.ProgramName;
        form.InstTypeCode = StudentApplicationFieldDefaults.ResolveInstTypeCode(match.InstTypeCode);
        form.ProgramCode = match.ProgramCode;
        return true;
    }

    private void ValidateForm(StudentApplicationFormModel form)
    {
        if (!form.ApplicationDate.HasValue)
        {
            ModelState.AddModelError("Form.ApplicationDate", "Application date is required.");
        }

        if (!form.DateOfBirth.HasValue)
        {
            ModelState.AddModelError("Form.DateOfBirth", "Date of birth is required.");
        }

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

    private static StudentApplicationFormModel CreateDefaultForm(StudentApplicationLookups lookups, string applicationNo)
    {
        return new StudentApplicationFormModel
        {
            ApplicationNo = applicationNo,
            ApplicationDate = DateTime.Today,
            DesiredYear = (short)DateTime.Today.Year,
            DesiredGradeOrSemester = 1,
            ApplicationStatus = lookups.ApplicationStatuses.FirstOrDefault() ?? "Pending",
            SourceChannel = lookups.SourceChannels.FirstOrDefault(s => string.Equals(s, "Online", StringComparison.OrdinalIgnoreCase))
                ?? lookups.SourceChannels.FirstOrDefault() ?? "Online",
            TestStatus = lookups.TestStatuses.FirstOrDefault() ?? "NotScheduled",
            PaymentStatus = lookups.PaymentStatuses.FirstOrDefault() ?? "Pending",
            Gender = lookups.Genders.FirstOrDefault() ?? "M"
        };
    }
}
