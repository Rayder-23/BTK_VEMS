using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

public class FeesController : StudentPortalBaseController
{
    private readonly IStudentProfileRepository _profiles;
    private readonly IStudentChallanRepository _challans;

    public FeesController(IStudentProfileRepository profiles, IStudentChallanRepository challans)
    {
        _profiles = profiles;
        _challans = challans;
    }

    public IActionResult CurrentMonth()
    {
        return RedirectToAction(nameof(Challan));
    }

    public async Task<IActionResult> Challan(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Challan";

        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        var model = await _challans.GetCurrentMonthChallanAsync(studentUid.Value, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> FeeHistory(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Fee History";

        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        var challans = await _challans.ListChallanHistoryAsync(studentUid.Value, cancellationToken);
        return View(new Models.StudentFeeHistoryPageModel { Challans = challans });
    }

    public IActionResult PreviousFee()
    {
        return RedirectToAction(nameof(FeeHistory));
    }
}
