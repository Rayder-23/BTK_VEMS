using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services.Admissions;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/admissions")]
public sealed class AdmissionsMgmtController : AdminBaseController
{
    private readonly IStudentApplicationAdminRepository _applications;

    public AdmissionsMgmtController(IStudentApplicationAdminRepository applications)
    {
        _applications = applications;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["PageTitle"] = "Admissions · Dashboard";
        var model = await _applications.GetDashboardAsync(cancellationToken);
        return View(model);
    }
}
