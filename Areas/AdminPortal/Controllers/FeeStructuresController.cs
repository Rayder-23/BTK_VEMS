using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/fee-structures")]
public sealed class FeeStructuresController : FeeMgmtControllerBase
{
    private readonly IFeeStructureRepository _structures;
    private readonly IFeeLookupRepository _lookups;

    public FeeStructuresController(IFeeStructureRepository structures, IFeeLookupRepository lookups)
    {
        _structures = structures;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(int? programId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Fee Structures";
        ViewData["PageTitle"] = "Fee Structures";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["ProgramId"] = programId;
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View(await _structures.ListAsync(programId, cancellationToken));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Fee Structure";
        ViewData["PageTitle"] = "Fee Structures · Add";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View(new FeeStructureFormModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeStructureFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Fee Structure";
        ViewData["PageTitle"] = "Fee Structures · Add";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await ValidateStructureAsync(model, null, cancellationToken))
        {
            return View(model);
        }

        var id = await _structures.InsertAsync(model, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Fee structure created (id {id}). Add line items on the details page.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _structures.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Fee Structure";
        ViewData["PageTitle"] = "Fee Structures · Edit";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeeStructureFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Fee Structure";
        ViewData["PageTitle"] = "Fee Structures · Edit";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);

        if (id != model.Uid)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await ValidateStructureAsync(model, id, cancellationToken))
        {
            return View(model);
        }

        var ok = await _structures.UpdateAsync(model, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Fee structure updated.";
        return RedirectToAction(nameof(Index), new { programId = model.ProgramId });
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, int? programId, CancellationToken cancellationToken)
    {
        var ok = await _structures.DeactivateAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Fee structure deactivated." : "Structure not found.";
        return RedirectToAction(nameof(Index), new { programId });
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var page = await _structures.GetDetailsPageAsync(id, cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Structure Details";
        ViewData["PageTitle"] = $"Structure · {page.Structure.StructureName}";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";
        ViewData["FeeHeads"] = await _lookups.GetActiveFeeHeadsAsync(cancellationToken);
        page.NewLine.StructureId = id;
        page.NewLine.ApplicableYear ??= page.Structure.AcademicYear;
        return View(page);
    }

    [HttpGet("details/{structureId:int}/edit-line/{detailId:int}")]
    public async Task<IActionResult> EditDetail(int structureId, int detailId, CancellationToken cancellationToken)
    {
        var line = await _structures.GetDetailAsync(detailId, cancellationToken);
        if (line is null || line.StructureId != structureId)
        {
            return NotFound();
        }

        var structures = await _structures.ListAsync(cancellationToken: cancellationToken);
        var structure = structures.FirstOrDefault(s => s.Uid == structureId);
        if (structure is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Fee Head";
        ViewData["PageTitle"] = $"Structure · {structure.StructureName} · Edit line";
        ViewData["FeeMgmtModuleKey"] = "FeeStructures";

        return View(new FeeStructureDetailEditPageModel
        {
            Structure = structure,
            Line = line,
            FeeHeads = await _lookups.GetActiveFeeHeadsAsync(cancellationToken)
        });
    }

    [HttpPost("details/{structureId:int}/edit-line/{detailId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDetail(int structureId, int detailId, FeeStructureDetailFormModel model, CancellationToken cancellationToken)
    {
        model.Uid = detailId;
        model.StructureId = structureId;

        if (!ModelState.IsValid || !ValidateDetailLine(model))
        {
            var structures = await _structures.ListAsync(cancellationToken: cancellationToken);
            var structure = structures.FirstOrDefault(s => s.Uid == structureId);
            if (structure is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Fee Head";
            ViewData["PageTitle"] = $"Structure · {structure.StructureName} · Edit line";
            ViewData["FeeMgmtModuleKey"] = "FeeStructures";

            return View(new FeeStructureDetailEditPageModel
            {
                Structure = structure,
                Line = model,
                FeeHeads = await _lookups.GetActiveFeeHeadsAsync(cancellationToken)
            });
        }

        if (await _structures.DetailExistsAsync(structureId, model.FeeHeadId, detailId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.FeeHeadId), "This fee head is already on the structure.");

            var structures = await _structures.ListAsync(cancellationToken: cancellationToken);
            var structure = structures.FirstOrDefault(s => s.Uid == structureId);
            if (structure is null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Edit Fee Head";
            ViewData["PageTitle"] = $"Structure · {structure.StructureName} · Edit line";
            ViewData["FeeMgmtModuleKey"] = "FeeStructures";

            return View(new FeeStructureDetailEditPageModel
            {
                Structure = structure,
                Line = model,
                FeeHeads = await _lookups.GetActiveFeeHeadsAsync(cancellationToken)
            });
        }

        var ok = await _structures.UpdateDetailAsync(model, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Line item updated.";
        return RedirectToAction(nameof(Details), new { id = structureId });
    }

    [HttpPost("details/{id:int}/add-line")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDetail(int id, FeeStructureDetailFormModel model, CancellationToken cancellationToken)
    {
        model.StructureId = id;
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid line item. Check fee head and amount.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ValidateDetailLine(model))
        {
            TempData["ErrorMessage"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Invalid billing frequency settings.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (await _structures.DetailExistsAsync(id, model.FeeHeadId, null, cancellationToken))
        {
            TempData["ErrorMessage"] = "This fee head is already on the structure.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _structures.AddDetailAsync(model, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = "Line item added.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("details/{structureId:int}/delete-line/{detailId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDetail(int structureId, int detailId, CancellationToken cancellationToken)
    {
        await _structures.DeleteDetailAsync(detailId, cancellationToken);
        TempData["StatusMessage"] = "Line item removed.";
        return RedirectToAction(nameof(Details), new { id = structureId });
    }

    [HttpGet("lookups/classes")]
    public async Task<IActionResult> ClassLookups(int programId, CancellationToken cancellationToken)
    {
        if (programId <= 0)
        {
            return BadRequest();
        }

        var classes = await _lookups.GetClassesByProgramAsync(programId, cancellationToken);
        return Json(classes.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            semester = c.Semester,
            academicYear = c.AcademicYear
        }));
    }

    private async Task<bool> ValidateStructureAsync(
        FeeStructureFormModel model,
        int? excludeUid,
        CancellationToken cancellationToken)
    {
        var normalizedClassId = model.ClassId is > 0 ? model.ClassId : null;

        if (normalizedClassId.HasValue)
        {
            var classes = await _lookups.GetClassesByProgramAsync(model.ProgramId, cancellationToken);
            if (classes.All(c => c.Id != normalizedClassId.Value))
            {
                ModelState.AddModelError(nameof(model.ClassId), "Select a valid class for this program.");
                return false;
            }
        }

        if (await _structures.ExistsAsync(
                model.ProgramId,
                model.Semester,
                model.AcademicYear,
                normalizedClassId,
                excludeUid,
                cancellationToken))
        {
            ModelState.AddModelError(
                string.Empty,
                normalizedClassId.HasValue
                    ? "A structure already exists for this program, class, semester, and academic year."
                    : "A structure already exists for this program, semester, and academic year.");
            return false;
        }

        model.ClassId = normalizedClassId;
        return true;
    }

    private bool ValidateDetailLine(FeeStructureDetailFormModel model)
    {
        var frequency = model.Frequency?.Trim() ?? string.Empty;
        if (!string.Equals(frequency, "Monthly", StringComparison.Ordinal)
            && !string.Equals(frequency, "OneTime", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Frequency), "Frequency must be Monthly or OneTime.");
            return false;
        }

        model.Frequency = frequency;

        if (string.Equals(frequency, "Monthly", StringComparison.Ordinal))
        {
            model.ApplicableMonth = null;
            model.ApplicableYear = null;
            return true;
        }

        if (!model.ApplicableMonth.HasValue || model.ApplicableMonth is < 1 or > 12)
        {
            ModelState.AddModelError(nameof(model.ApplicableMonth), "Select a valid month (1–12) for one-time fees.");
            return false;
        }

        if (!model.ApplicableYear.HasValue)
        {
            ModelState.AddModelError(nameof(model.ApplicableYear), "Applicable year is required for one-time fees.");
            return false;
        }

        return true;
    }
}
