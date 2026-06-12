using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Fee;
using VEMS.Areas.AdminPortal.Services;
using VEMS.Areas.AdminPortal.Services.Fee;
using VEMS.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/payments")]
public sealed class FeePaymentsController : FeeMgmtControllerBase
{
    private const string BanksConfigKey = "Banks";

    private readonly IFeePaymentRepository _payments;
    private readonly IFeeLookupRepository _lookups;
    private readonly IConfigurationValuesProvider _configurations;

    public FeePaymentsController(
        IFeePaymentRepository payments,
        IFeeLookupRepository lookups,
        IConfigurationValuesProvider configurations)
    {
        _payments = payments;
        _lookups = lookups;
        _configurations = configurations;
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
        await SetCreateViewDataAsync(cancellationToken);

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
        await SetCreateViewDataAsync(cancellationToken);
        var banks = (IReadOnlyList<string>)(ViewData["Banks"] ?? Array.Empty<string>());

        var challan = await _payments.GetChallanForPaymentAsync(model.ChallanId, cancellationToken);
        if (challan is null)
        {
            ModelState.AddModelError(nameof(model.ChallanId), "Challan not found or not payable.");
        }

        if (!string.IsNullOrWhiteSpace(model.BankName)
            && banks.Count > 0
            && !banks.Contains(model.BankName, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.BankName), "Select a valid bank.");
        }

        if (!ModelState.IsValid)
        {
            MergeChallanSummary(model, challan);
            return View(model);
        }

        if (challan is null)
        {
            return View(model);
        }

        if (model.AmountPaid > challan.Balance)
        {
            ModelState.AddModelError(nameof(model.AmountPaid), $"Amount cannot exceed balance ({challan.Balance:N2}).");
            MergeChallanSummary(model, challan);
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
            MergeChallanSummary(model, challan);
            return View(model);
        }
    }

    private async Task SetCreateViewDataAsync(CancellationToken cancellationToken)
    {
        ViewData["Challans"] = await _lookups.GetUnpaidChallansAsync(cancellationToken);
        ViewData["Banks"] = await _configurations.GetValuesAsync(BanksConfigKey, cancellationToken);
    }

    private static void MergeChallanSummary(PaymentFormModel model, PaymentFormModel? challan)
    {
        if (challan is null)
        {
            return;
        }

        model.ChallanNo = challan.ChallanNo;
        model.StudentName = challan.StudentName;
        model.ApplicantId = challan.ApplicantId;
        model.IsApplicationChallan = challan.IsApplicationChallan;
        model.NetPayable = challan.NetPayable;
        model.AmountPaidSoFar = challan.AmountPaidSoFar;
    }
}
