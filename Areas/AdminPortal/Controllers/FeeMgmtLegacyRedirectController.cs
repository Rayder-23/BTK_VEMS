using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

/// <summary>Redirects legacy fee/challan URLs into the fee management area.</summary>
public sealed class FeeMgmtLegacyRedirectController : AdminBaseController
{
    [HttpGet("/adminportal/challans")]
    [HttpGet("/adminportal/challans/{*path}")]
    public IActionResult Challans() =>
        RedirectPermanent("/adminportal/fee/challans");
}
