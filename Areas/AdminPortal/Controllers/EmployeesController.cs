using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class EmployeesController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Employees";
        ViewData["PageTitle"] = "Employees";
        return View(AdminModuleCatalog.CreateModulePage("Employees"));
    }
}
