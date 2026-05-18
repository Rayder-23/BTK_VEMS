using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/concessions")]
public sealed class FeeConcessionsController : FeeMgmtControllerBase
{
    private readonly IFeeConcessionRepository _concessions;
    private readonly IFeeLookupRepository _lookups;

    public FeeConcessionsController(IFeeConcessionRepository concessions, IFeeLookupRepository lookups)
    {
        _concessions = concessions;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Concessions";
        ViewData["PageTitle"] = "Concessions";
        ViewData["FeeMgmtModuleKey"] = "Concessions";
        return View(await _concessions.ListAsync(cancellationToken));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Concession";
        ViewData["PageTitle"] = "Concessions · Add";
        ViewData["FeeMgmtModuleKey"] = "Concessions";
        await LoadLookupsAsync(cancellationToken);
        return View(new ConcessionFormModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ConcessionFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Concession";
        ViewData["PageTitle"] = "Concessions · Add";
        ViewData["FeeMgmtModuleKey"] = "Concessions";
        await LoadLookupsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        NormalizeConcession(model);
        var id = await _concessions.InsertAsync(model, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Concession created (id {id}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _concessions.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Concession";
        ViewData["PageTitle"] = "Concessions · Edit";
        ViewData["FeeMgmtModuleKey"] = "Concessions";
        await LoadLookupsAsync(cancellationToken);
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ConcessionFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Concession";
        ViewData["PageTitle"] = "Concessions · Edit";
        ViewData["FeeMgmtModuleKey"] = "Concessions";
        await LoadLookupsAsync(cancellationToken);

        if (id != model.Uid)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        NormalizeConcession(model);
        var ok = await _concessions.UpdateAsync(model, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Concession updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("deactivate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var ok = await _concessions.DeactivateAsync(id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Concession deactivated." : "Concession not found.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        ViewData["Students"] = await _lookups.GetActiveStudentsAsync(cancellationToken);
        ViewData["FeeHeads"] = await _lookups.GetActiveFeeHeadsAsync(cancellationToken);
    }

    private static void NormalizeConcession(ConcessionFormModel model)
    {
        if (string.Equals(model.ConcessionType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            model.DiscountAmount = 0;
        }
        else
        {
            model.DiscountPercent = 0;
            if (!string.Equals(model.ConcessionType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                model.ConcessionType = "FixedAmount";
            }
        }
    }
}
