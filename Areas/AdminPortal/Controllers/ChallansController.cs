using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class ChallansController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Challans";
        ViewData["PageTitle"] = "Challans";
        return View(AdminModuleCatalog.CreateModulePage("Challans"));
    }
}
