using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services.Admissions;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/admissions/summary")]
public sealed class AdmissionsSummaryController : AdminBaseController
{
    private readonly IStudentApplicationAdminRepository _applications;

    public AdmissionsSummaryController(IStudentApplicationAdminRepository applications)
    {
        _applications = applications;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Summary";
        ViewData["PageTitle"] = "Admissions · Summary";
        var model = await _applications.GetSummaryAsync(cancellationToken);
        return View(model);
    }
}
