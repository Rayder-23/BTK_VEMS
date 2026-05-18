using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/payment-receipts")]
public sealed class FeePaymentReceiptsController : FeeMgmtControllerBase
{
    private readonly IFeePaymentRepository _payments;

    public FeePaymentReceiptsController(IFeePaymentRepository payments)
    {
        _payments = payments;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Receipts";
        ViewData["PageTitle"] = "Payment Receipts";
        ViewData["FeeMgmtModuleKey"] = "PaymentReceipts";
        return View(await _payments.ListReceiptsAsync(cancellationToken));
    }

    [HttpGet("print/{id:int}")]
    public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
    {
        var receipt = await _payments.GetReceiptAsync(id, cancellationToken);
        if (receipt is null)
        {
            return NotFound();
        }

        await _payments.IncrementPrintCountAsync(id, cancellationToken);
        ViewData["Title"] = $"Receipt {receipt.ReceiptNo}";
        ViewData["FeeMgmtModuleKey"] = "PaymentReceipts";
        return View(receipt);
    }
}
