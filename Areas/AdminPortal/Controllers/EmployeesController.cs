using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

public class EmployeesController : AdminBaseController
{
    private readonly IEmployeeRepository _employees;

    public EmployeesController(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Employees";
        ViewData["PageTitle"] = "Employees";
        var items = await _employees.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add employee";
        ViewData["PageTitle"] = "Employees · Add";
        return View(new EmployeeFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add employee";
        ViewData["PageTitle"] = "Employees · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApplyAuditOnWrite(model);
        var newId = await _employees.InsertAsync(model, cancellationToken);
        TempData["StatusMessage"] = $"Employee created (internal id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit employee";
        ViewData["PageTitle"] = "Employees · Edit";

        var row = await _employees.GetAsync(id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit employee";
        ViewData["PageTitle"] = "Employees · Edit";

        if (model.Uid <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ApplyAuditOnWrite(model);
        var ok = await _employees.UpdateAsync(model, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Employee updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _employees.DeleteAsync(id, cancellationToken);
        TempData["StatusMessage"] = ok
            ? "Employee deleted."
            : "Employee could not be deleted (record not found).";
        return RedirectToAction(nameof(Index));
    }

    private void ApplyAuditOnWrite(EmployeeFormModel model)
    {
        var admin = HttpContext.Session.GetString(LoginController.AdminSessionKey);
        if (string.IsNullOrWhiteSpace(admin))
        {
            return;
        }

        model.ModifiedBy = admin.Trim();
        model.ModifiedOn = DateTime.Now.ToString("O");
    }
}
