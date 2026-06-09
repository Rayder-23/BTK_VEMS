using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.StudentPortal.Controllers;

public class FeesController : StudentPortalBaseController
{
    public IActionResult CurrentMonth()
    {
        ViewData["Title"] = "Current Month Fee";
        return View();
    }

    public IActionResult PreviousFee()
    {
        ViewData["Title"] = "Previous Fee";
        return View();
    }
}
