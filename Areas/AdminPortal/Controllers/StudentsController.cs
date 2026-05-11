using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class StudentsController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Students";
        ViewData["PageTitle"] = "Students";
        return View(AdminModuleCatalog.CreateModulePage("Students"));
    }
}
