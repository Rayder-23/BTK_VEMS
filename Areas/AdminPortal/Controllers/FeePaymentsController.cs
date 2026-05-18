using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/payments")]
public sealed class FeePaymentsController : FeeMgmtControllerBase
{
    private readonly IFeePaymentRepository _payments;
    private readonly IFeeLookupRepository _lookups;

    public FeePaymentsController(IFeePaymentRepository payments, IFeeLookupRepository lookups)
    {
        _payments = payments;
        _lookups = lookups;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Payments";
        ViewData["PageTitle"] = "Payments";
        ViewData["FeeMgmtModuleKey"] = "Payments";
        return View(await _payments.ListAsync(cancellationToken));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? challanId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Record Payment";
        ViewData["PageTitle"] = "Payments · Record";
        ViewData["FeeMgmtModuleKey"] = "Payments";
        ViewData["Challans"] = await _lookups.GetUnpaidChallansAsync(cancellationToken);

        PaymentFormModel model;
        if (challanId is > 0)
        {
            model = await _payments.GetChallanForPaymentAsync(challanId.Value, cancellationToken)
                ?? new PaymentFormModel { ChallanId = challanId.Value };
        }
        else
        {
            model = new PaymentFormModel();
        }

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Record Payment";
        ViewData["PageTitle"] = "Payments · Record";
        ViewData["FeeMgmtModuleKey"] = "Payments";
        ViewData["Challans"] = await _lookups.GetUnpaidChallansAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var challan = await _payments.GetChallanForPaymentAsync(model.ChallanId, cancellationToken);
        if (challan is null)
        {
            ModelState.AddModelError(nameof(model.ChallanId), "Challan not found or not payable.");
            return View(model);
        }

        if (model.AmountPaid > challan.Balance)
        {
            ModelState.AddModelError(nameof(model.AmountPaid), $"Amount cannot exceed balance ({challan.Balance:N2}).");
            return View(model);
        }

        try
        {
            var paymentId = await _payments.RecordPaymentAsync(model, ResolveActorId(), cancellationToken);
            TempData["StatusMessage"] = "Payment recorded.";
            return RedirectToAction("Print", "FeePaymentReceipts", new { id = paymentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
