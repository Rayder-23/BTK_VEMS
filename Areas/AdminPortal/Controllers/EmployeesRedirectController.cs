using Microsoft.AspNetCore.Mvc;

namespace VEMS.Areas.AdminPortal.Controllers;

/// <summary>Redirects legacy /adminportal/employees URLs to the HR module.</summary>
public sealed class EmployeesRedirectController : AdminBaseController
{
    [HttpGet]
    [Route("adminportal/employees")]
    [Route("adminportal/employees/{action}")]
    [Route("adminportal/employees/{action}/{id:int}")]
    public IActionResult RedirectLegacy(string? action, int? id)
    {
        var target = action?.ToLowerInvariant() switch
        {
            "create" => "/adminportal/hr/employees/create",
            "edit" when id.HasValue => $"/adminportal/hr/employees/edit/{id.Value}",
            _ => "/adminportal/hr/employees"
        };

        return RedirectPermanent(target);
    }
}
