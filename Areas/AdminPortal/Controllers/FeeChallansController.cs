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
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Challans Management";
        ViewData["PageTitle"] = "Challans Management";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Search"] = search;
        return View(await _challans.ListAsync(search, cancellationToken));
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

    [HttpGet("print-voucher")]
    public async Task<IActionResult> PrintVoucher(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Print voucher";
        ViewData["PageTitle"] = "Challans · Print voucher";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        var list = await _challans.ListAsync(null, cancellationToken);
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
}
