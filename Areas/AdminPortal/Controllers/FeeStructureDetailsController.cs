using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/fee/fee-structure-details")]
public sealed class FeeStructureDetailsController : FeeMgmtControllerBase
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => RedirectToAction("Index", "FeeStructures");
}
