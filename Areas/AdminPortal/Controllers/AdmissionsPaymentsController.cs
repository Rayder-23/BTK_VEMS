using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services.Admissions;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/admissions/payments")]
public sealed class AdmissionsPaymentsController : AdminBaseController
{
    private readonly IStudentApplicationAdminRepository _applications;

    public AdmissionsPaymentsController(IStudentApplicationAdminRepository applications)
    {
        _applications = applications;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, string? paymentStatus, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Payments";
        ViewData["PageTitle"] = "Admissions · Payments";
        ViewData["Search"] = search;
        ViewData["PaymentStatus"] = paymentStatus;
        var lookups = await _applications.GetLookupsAsync(cancellationToken);
        ViewData["PaymentStatusOptions"] = lookups.PaymentStatuses;
        var items = await _applications.ListPaymentsAsync(search, paymentStatus, cancellationToken);
        return View(items);
    }
}
