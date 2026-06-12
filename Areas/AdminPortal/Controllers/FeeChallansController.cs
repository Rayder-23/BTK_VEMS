using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/challans")]
public sealed class FeeChallansController : FeeMgmtControllerBase
{
    private readonly IFeeChallanRepository _challans;
    private readonly IFeeLookupRepository _lookups;

    public FeeChallansController(IFeeChallanRepository challans, IFeeLookupRepository lookups)
    {
        _challans = challans;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, int? programId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Challans Management";
        ViewData["PageTitle"] = "Challans Management";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Search"] = search;
        ViewData["ProgramId"] = programId;
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View(await _challans.ListAsync(search, programId, cancellationToken));
    }

    [HttpGet("bulk")]
    public async Task<IActionResult> Bulk(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Bulk Generate Challans";
        ViewData["PageTitle"] = "Challans · Bulk Generate";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View();
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Generate Challan";
        ViewData["PageTitle"] = "Challans · Generate";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Students"] = await _lookups.GetActiveStudentsAsync(cancellationToken);
        ViewData["Structures"] = await _lookups.GetActiveStructuresAsync(cancellationToken);
        return View(new ChallanGenerateFormModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChallanGenerateFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Generate Challan";
        ViewData["PageTitle"] = "Challans · Generate";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Students"] = await _lookups.GetActiveStudentsAsync(cancellationToken);
        ViewData["Structures"] = await _lookups.GetActiveStructuresAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var id = await _challans.GenerateChallanAsync(model, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = "Challan generated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var page = await _challans.GetDetailsAsync(id, cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Challan Details";
        ViewData["PageTitle"] = $"Challan · {page.Header.ChallanNo}";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        return View(page);
    }

    [HttpPost("cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var ok = await _challans.CancelAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Challan cancelled." : "Challan could not be cancelled (not found or already paid).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _challans.DeleteCancelledAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok
            ? "Cancelled challan deleted."
            : "Challan could not be deleted. It must be cancelled with no payments recorded.";
        return ok ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("print-voucher")]
    public async Task<IActionResult> PrintVoucher(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Print voucher";
        ViewData["PageTitle"] = "Challans · Print voucher";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        var list = await _challans.ListAsync(null, null, cancellationToken);
        return View(list);
    }

    [HttpGet("print/{id:int}")]
    public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
    {
        var page = await _challans.GetDetailsAsync(id, cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        return View(page);
    }

    [HttpGet("lookup-students")]
    public async Task<IActionResult> LookupStudents(int programId, CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        var feeContext = await _lookups.ResolveProgramBulkChallanContextAsync(programId, cancellationToken);
        if (feeContext is null)
        {
            return BadRequest(new { message = "No active fee structure with line items found for this program." });
        }

        var students = await _challans.GetEligibleStudentsAsync(
            programId,
            feeContext.Semester,
            feeContext.AcademicYear,
            cancellationToken);

        return Json(new
        {
            feeContext = new
            {
                structureId = feeContext.StructureId,
                structureName = feeContext.StructureName,
                semester = feeContext.Semester,
                academicYear = feeContext.AcademicYear
            },
            students = students.Select(s => new
            {
                studentId = s.StudentId,
                registrationNo = s.RegistrationNo,
                rollNo = s.RollNo,
                studentName = s.StudentName,
                programName = s.ProgramName,
                hasConcession = s.HasConcession,
                alreadyHasChallan = s.AlreadyHasChallan
            })
        });
    }

    [HttpPost("bulk-generate")]
    public async Task<IActionResult> BulkGenerate([FromBody] BulkChallanApiRequest body, CancellationToken cancellationToken)
    {
        if (body.ProgramId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        if (body.IssueDate > body.DueDate)
        {
            return BadRequest(new { message = "Issue date must be on or before due date." });
        }

        if (body.StudentIds is not { Count: > 0 })
        {
            return BadRequest(new { message = "Select at least one student." });
        }

        var feeContext = await _lookups.ResolveProgramBulkChallanContextAsync(body.ProgramId, cancellationToken);
        if (feeContext is null)
        {
            return BadRequest(new { message = "No active fee structure with line items found for this program." });
        }

        var request = new BulkChallanGenerateRequest
        {
            ProgramId = body.ProgramId,
            StructureId = feeContext.StructureId,
            Semester = feeContext.Semester,
            AcademicYear = feeContext.AcademicYear,
            IssueDate = body.IssueDate,
            DueDate = body.DueDate,
            CreatedBy = ResolveActorId(),
            StudentIds = body.StudentIds
        };

        try
        {
            var response = await _challans.BulkGenerateAsync(request, cancellationToken);
            return Json(new
            {
                totalProcessed = response.TotalProcessed,
                totalGenerated = response.TotalGenerated,
                totalSkipped = response.TotalSkipped,
                totalErrors = response.TotalErrors,
                results = response.Results.Select(r => new
                {
                    studentId = r.StudentId,
                    registrationNo = r.RegistrationNo,
                    studentName = r.StudentName,
                    challanNo = r.ChallanNo,
                    netPayable = r.NetPayable,
                    status = r.Status
                })
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class BulkChallanApiRequest
    {
        public int ProgramId { get; set; }
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(15));
        public IReadOnlyList<int>? StudentIds { get; set; }
    }
}
