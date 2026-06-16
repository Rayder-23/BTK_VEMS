using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/challans-multimonth")]
public sealed class FeeChallanMultiMonthController : FeeMgmtControllerBase
{
    private readonly IFeeChallanRepository _challans;
    private readonly IFeeLookupRepository _lookups;

    public FeeChallanMultiMonthController(IFeeChallanRepository challans, IFeeLookupRepository lookups)
    {
        _challans = challans;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, int? programId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Challan MultiMonth";
        ViewData["PageTitle"] = "Challan MultiMonth";
        ViewData["FeeMgmtModuleKey"] = "Challans";
        ViewData["Search"] = search;
        ViewData["ProgramId"] = programId;
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        return View(await _challans.ListAsync(search, programId, cancellationToken));
    }
}
