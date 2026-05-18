using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/fee-heads")]
public sealed class FeeRefFeeHeadsController : FeeMgmtControllerBase
{
    private readonly IFeeHeadRepository _feeHeads;

    public FeeRefFeeHeadsController(IFeeHeadRepository feeHeads)
    {
        _feeHeads = feeHeads;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool showInactive = false, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Fee Heads";
        ViewData["PageTitle"] = "Fee Heads";
        ViewData["FeeMgmtModuleKey"] = "RefFeeHeads";
        ViewData["Search"] = search;
        ViewData["ShowInactive"] = showInactive;
        var items = await _feeHeads.ListAsync(search, activeOnly: !showInactive, cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add Fee Head";
        ViewData["PageTitle"] = "Fee Heads · Add";
        ViewData["FeeMgmtModuleKey"] = "RefFeeHeads";
        return View(new FeeHeadFormModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeHeadFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add Fee Head";
        ViewData["PageTitle"] = "Fee Heads · Add";
        ViewData["FeeMgmtModuleKey"] = "RefFeeHeads";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _feeHeads.HeadCodeExistsAsync(model.HeadCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.HeadCode), "Head code already exists.");
            return View(model);
        }

        var id = await _feeHeads.InsertAsync(model, ResolveActorId(), cancellationToken);
        TempData["StatusMessage"] = $"Fee head created (id {id}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var row = await _feeHeads.GetAsync((short)id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Edit Fee Head";
        ViewData["PageTitle"] = "Fee Heads · Edit";
        ViewData["FeeMgmtModuleKey"] = "RefFeeHeads";
        return View(row);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeeHeadFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Fee Head";
        ViewData["PageTitle"] = "Fee Heads · Edit";
        ViewData["FeeMgmtModuleKey"] = "RefFeeHeads";

        if (id != model.Uid)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _feeHeads.HeadCodeExistsAsync(model.HeadCode, (short)id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.HeadCode), "Head code already exists.");
            return View(model);
        }

        var ok = await _feeHeads.UpdateAsync(model, ResolveStaffLoginUid(), cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Fee head updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _feeHeads.DeactivateAsync((short)id, ResolveStaffLoginUid(), cancellationToken);
        TempData["StatusMessage"] = ok ? "Fee head deactivated." : "Fee head not found.";
        return RedirectToAction(nameof(Index));
    }
}
