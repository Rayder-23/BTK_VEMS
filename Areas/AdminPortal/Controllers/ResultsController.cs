using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class ResultsController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Results";
        ViewData["PageTitle"] = "Results";
        return View(AdminModuleCatalog.CreateModulePage("Results"));
    }
}
