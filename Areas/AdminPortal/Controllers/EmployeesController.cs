using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using VEMS.Areas.AdminPortal.Models;
using VEMS.Areas.AdminPortal.Services;

namespace VEMS.Areas.AdminPortal.Controllers;

[Route("adminportal/hr/employees")]
public sealed class EmployeesController : HrBaseController
{
    private readonly IEmployeeRepository _employees;

    public EmployeesController(IEmployeeRepository employees)
    {
        _employees = employees;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Employees";
        ViewData["PageTitle"] = "Employees";
        var items = await _employees.ListAsync(cancellationToken);
        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Add employee";
        ViewData["PageTitle"] = "Employees · Add";
        return View(new EmployeeFormModel
        {
            JoinedDate = DateTime.Today,
            Status = "Active"
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Add employee";
        ViewData["PageTitle"] = "Employees · Add";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var newId = await _employees.InsertAsync(model, cancellationToken);
        TempData["StatusMessage"] = $"Employee created (internal id {newId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
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

    [HttpPost("edit/{id:int}")]
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

        var ok = await _employees.UpdateAsync(model, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Employee updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await _employees.DeleteAsync(id, cancellationToken);
            TempData["StatusMessage"] = ok
                ? "Employee deleted."
                : "Employee could not be deleted (record not found).";
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData["ErrorMessage"] = "Employee could not be deleted because login or other records still reference this employee.";
        }

        return RedirectToAction(nameof(Index));
    }
}
