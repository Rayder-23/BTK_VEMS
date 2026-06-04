using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/accounts")]
public sealed class AccountsController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Accounts";
        ViewData["PageTitle"] = "Accounts";
        return View(AdminModuleCatalog.CreateModulePage("Accounts"));
    }
}
