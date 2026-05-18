using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/challan-details")]
public sealed class FeeChallanDetailsController : FeeMgmtControllerBase
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => RedirectToAction("Index", "FeeChallans");
}
