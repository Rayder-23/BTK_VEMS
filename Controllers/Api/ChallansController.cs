using System.Security.Claims;
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

    [HttpGet("bulk-eligible-students")]
    public async Task<IActionResult> GetEligibleStudents(
        [FromQuery] int programId,
        [FromQuery] string semester,
        [FromQuery] short academicYear,
        CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        if (string.IsNullOrWhiteSpace(semester))
        {
            return BadRequest(new { message = "Semester is required." });
        }

        if (academicYear is < 1900 or > 9999)
        {
            return BadRequest(new { message = "Academic year must be a valid 4-digit year." });
        }

        var students = await _challans.GetEligibleStudentsAsync(programId, semester, academicYear, cancellationToken);
        return Ok(students);
    }

    [HttpGet("structures")]
    public async Task<IActionResult> GetStructures(
        [FromQuery] int programId,
        [FromQuery] string? semester,
        [FromQuery] short? academicYear,
        CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest(new { message = "Program is required." });
        }

        var structures = await _lookups.GetActiveStructuresByProgramAsync(programId, semester, academicYear, cancellationToken);
        return Ok(structures);
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

        if (body.StructureId <= 0)
        {
            return BadRequest(new { message = "Fee structure is required." });
        }

        if (string.IsNullOrWhiteSpace(body.Semester))
        {
            return BadRequest(new { message = "Semester is required." });
        }

        if (body.AcademicYear is < 1900 or > 9999)
        {
            return BadRequest(new { message = "Academic year must be a valid 4-digit year." });
        }

        if (body.IssueDate > body.DueDate)
        {
            return BadRequest(new { message = "Issue date must be on or before due date." });
        }

        if (body.StudentIds is { Count: > 0 } && body.StudentIds.All(id => id <= 0))
        {
            return BadRequest(new { message = "At least one student must be selected." });
        }

        var request = new BulkChallanGenerateRequest
        {
            ProgramId = body.ProgramId,
            StructureId = body.StructureId,
            Semester = body.Semester,
            AcademicYear = body.AcademicYear,
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
        public string Semester { get; set; } = "Fall";
        public short AcademicYear { get; set; } = (short)DateTime.Today.Year;
        public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(15));
        public IReadOnlyList<int>? StudentIds { get; set; }
    }
}
