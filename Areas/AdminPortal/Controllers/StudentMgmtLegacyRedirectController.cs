using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

/// <summary>Redirects legacy admin URLs into the student management area.</summary>
public sealed class StudentMgmtLegacyRedirectController : AdminBaseController
{
    [HttpGet("/adminportal/students/create")]
    public IActionResult StudentsCreate() =>
        RedirectPermanent("/adminportal/students/students/create");

    [HttpGet("/adminportal/students/edit/{id:int}")]
    public IActionResult StudentsEdit(int id) =>
        RedirectPermanent($"/adminportal/students/students/edit/{id}");

    [HttpGet("/adminportal/courses")]
    [HttpGet("/adminportal/courses/{*path}")]
    public IActionResult Courses() =>
        RedirectPermanent("/adminportal/students/courses");

    [HttpGet("/adminportal/attendance")]
    [HttpGet("/adminportal/attendance/{*path}")]
    public IActionResult Attendance() =>
        RedirectPermanent("/adminportal/students/attendance");

    [HttpGet("/adminportal/results")]
    [HttpGet("/adminportal/results/{*path}")]
    public IActionResult Results() =>
        RedirectPermanent("/adminportal/students/results");

}
