using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/pay-challans")]
public sealed class FeePayChallansController : FeeMgmtControllerBase
{
    private readonly IFeeChallanRepository _challans;
    private readonly IFeeLookupRepository _lookups;

    public FeePayChallansController(IFeeChallanRepository challans, IFeeLookupRepository lookups)
    {
        _challans = challans;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(
        string? search,
        int? programId,
        string? billingPeriod,
        string? paymentStatus,
        CancellationToken cancellationToken)
    {
        string? challanMonth = null;
        string? challanYear = null;
        if (!string.IsNullOrWhiteSpace(billingPeriod))
        {
            var parts = billingPeriod.Split('|', 2);
            challanMonth = parts[0].Trim();
            challanYear = parts.Length > 1 ? parts[1].Trim() : null;
        }

        var normalizedPaymentStatus = NormalizePaymentStatus(paymentStatus);

        ViewData["Title"] = "Pay Challans";
        ViewData["PageTitle"] = "Pay Challans";
        ViewData["FeeMgmtModuleKey"] = "Pay Challans";
        ViewData["Search"] = search;
        ViewData["ProgramId"] = programId;
        ViewData["BillingPeriod"] = billingPeriod;
        ViewData["PaymentStatus"] = normalizedPaymentStatus;
        ViewData["Programs"] = await _lookups.GetProgramsAsync(cancellationToken);
        ViewData["BillingPeriods"] = await _challans.GetDistinctBillingPeriodsAsync(cancellationToken);

        var items = await _challans.ListAsync(
            search,
            programId,
            challanMonth,
            challanYear,
            normalizedPaymentStatus,
            cancellationToken);

        return View(items);
    }

    private static string? NormalizePaymentStatus(string? paymentStatus)
    {
        if (string.IsNullOrWhiteSpace(paymentStatus))
        {
            return null;
        }

        return paymentStatus.Trim() switch
        {
            "Paid" => "Paid",
            "Unpaid" => "Unpaid",
            "PartiallyPaid" or "Partially Paid" => "PartiallyPaid",
            _ => null
        };
    }
}
