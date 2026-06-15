using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Models;
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

        var profile = await _profiles.GetByStudentUidAsync(studentUid.Value, cancellationToken);
        var page = await _challans.GetCurrentMonthChallanAsync(studentUid.Value, cancellationToken);
        return View(WithStudentIdentity(page, profile));
    }

    public async Task<IActionResult> PrintChallan(int id, CancellationToken cancellationToken)
    {
        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        var profile = await _profiles.GetByStudentUidAsync(studentUid.Value, cancellationToken);
        var page = await _challans.GetChallanForStudentAsync(studentUid.Value, id, cancellationToken);
        if (page?.Challan is null)
        {
            return NotFound();
        }

        return View(WithStudentIdentity(page, profile));
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

    private static StudentChallanPageModel WithStudentIdentity(
        StudentChallanPageModel page,
        StudentProfileViewModel? profile) =>
        new()
        {
            Challan = page.Challan,
            Lines = page.Lines,
            StudentName = profile?.FullName ?? string.Empty,
            RegistrationNo = profile?.RegistrationNo ?? string.Empty
        };
}
