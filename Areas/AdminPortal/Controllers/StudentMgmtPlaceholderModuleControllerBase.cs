using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public abstract class StudentMgmtPlaceholderModuleControllerBase : StudentMgmtBaseController
{
    private StudentMgmtModule Module => StudentMgmtModuleCatalog.Get(ModuleKey);

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = Module.Name;
        ViewData["PageTitle"] = Module.Name;
        return View("StudentMgmtModuleIndex", StudentMgmtModuleCatalog.CreateListPage(ModuleKey));
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = $"{Module.Name} · Add";
        ViewData["PageTitle"] = ViewData["Title"];
        return View("StudentMgmtModuleForm", StudentMgmtModuleCatalog.CreateFormPage(ModuleKey));
    }

    [HttpGet("edit/{id:int}")]
    public IActionResult Edit(int id)
    {
        ViewData["Title"] = $"{Module.Name} · Edit";
        ViewData["PageTitle"] = ViewData["Title"];
        return View("StudentMgmtModuleForm", StudentMgmtModuleCatalog.CreateFormPage(ModuleKey, id));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePost()
    {
        TempData["StatusMessage"] =
            $"{Module.Name} save is not connected to a database yet. Schema and repositories can be added when tables are ready.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditPost(int id)
    {
        TempData["StatusMessage"] =
            $"{Module.Name} update for record #{id} is not connected to a database yet.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        TempData["StatusMessage"] =
            $"{Module.Name} delete for record #{id} is not connected to a database yet.";
        return RedirectToAction(nameof(Index));
    }
}
