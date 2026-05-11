using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class CoursesController : AdminBaseController
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Courses";
        ViewData["PageTitle"] = "Courses";
        return View(AdminModuleCatalog.CreateModulePage("Courses"));
    }
}
