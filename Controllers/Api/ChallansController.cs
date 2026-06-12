using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = AdminPortalAuth.Scheme)]
[Route("api/challans")]
public sealed class ChallansController : ControllerBase
{
    private readonly IFeeChallanRepository _challans;
    private readonly IFeeLookupRepository _lookups;

    public ChallansController(IFeeChallanRepository challans, IFeeLookupRepository lookups)
    {
        _challans = challans;
        _lookups = lookups;
    }

    [HttpGet("program-structures")]
    public async Task<IActionResult> GetProgramStructures(
        [FromQuery] int programId,
        CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        var structures = await _lookups.GetProgramStructuresAsync(programId, cancellationToken);
        return Ok(structures.Select(s => new
        {
            id = s.StructureId,
            name = $"{s.StructureName} ({s.Semester} {s.AcademicYear})",
            semester = s.Semester,
            academicYear = s.AcademicYear
        }));
    }

    [HttpGet("bulk-eligible-students")]
    public async Task<IActionResult> GetEligibleStudents(
        [FromQuery] int programId,
        [FromQuery] int structureId,
        CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        var feeContext = structureId > 0
            ? await _lookups.ResolveStructureBulkChallanContextAsync(programId, structureId, cancellationToken)
            : await _lookups.ResolveProgramBulkChallanContextAsync(programId, cancellationToken);
        if (feeContext is null)
        {
            return BadRequest(new { message = "No active fee structure with line items found for this program." });
        }

        var students = await _challans.GetEligibleStudentsAsync(
            programId,
            feeContext.Semester,
            feeContext.AcademicYear,
            cancellationToken);

        return Ok(new
        {
            feeContext = new
            {
                structureId = feeContext.StructureId,
                structureName = feeContext.StructureName,
                semester = feeContext.Semester,
                academicYear = feeContext.AcademicYear
            },
            students
        });
    }

    [HttpPost("bulk-generate")]
    public async Task<IActionResult> BulkGenerate(
        [FromBody] BulkChallanApiRequest body,
        CancellationToken cancellationToken)
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

        var feeContext = body.StructureId > 0
            ? await _lookups.ResolveStructureBulkChallanContextAsync(body.ProgramId, body.StructureId, cancellationToken)
            : await _lookups.ResolveProgramBulkChallanContextAsync(body.ProgramId, cancellationToken);
        if (feeContext is null)
        {
            return BadRequest(new { message = "Selected fee structure is invalid for this program, or no active fee structure with line items exists." });
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
            return Ok(new
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

    private int ResolveActorId()
    {
        var claim = User.FindFirst(AdminPortalAuth.EmployeeLoginUidClaim)?.Value;
        return int.TryParse(claim, out var uid) && uid > 0 ? uid : 1;
    }

    public sealed class BulkChallanApiRequest
    {
        public int ProgramId { get; set; }
        public int StructureId { get; set; }
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(15));
        public IReadOnlyList<int>? StudentIds { get; set; }
    }
}
