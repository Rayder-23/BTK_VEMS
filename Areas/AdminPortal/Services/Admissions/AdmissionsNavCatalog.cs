namespace VEMS.Areas.AdminPortal.Services.Admissions;

public sealed class AdmissionsNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class AdmissionsNavCatalog
{
    public static IReadOnlyList<AdmissionsNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/admissions", IconClass = "fa-gauge-high" },
        new() { Key = "add", Name = "Add Applicant", Url = "/adminportal/admissions/applications/create", IconClass = "fa-user-plus" },
        new() { Key = "all", Name = "All Applicants", Url = "/adminportal/admissions/applications", IconClass = "fa-users" },
        new() { Key = "payments", Name = "Payments", Url = "/adminportal/admissions/payments", IconClass = "fa-credit-card" },
        new() { Key = "summary", Name = "Summary", Url = "/adminportal/admissions/summary", IconClass = "fa-chart-pie" }
    ];

    private static readonly HashSet<string> AdmissionsControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "AdmissionsMgmt",
        "StudentApplications",
        "AdmissionsPayments",
        "AdmissionsSummary"
    };

    public static bool IsAdmissionsController(string controller) => AdmissionsControllers.Contains(controller);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/applications/create", StringComparison.Ordinal))
        {
            return "add";
        }

        if (path.Contains("/payments", StringComparison.Ordinal))
        {
            return "payments";
        }

        if (path.Contains("/summary", StringComparison.Ordinal))
        {
            return "summary";
        }

        if (path.Contains("/applications", StringComparison.Ordinal)
            && (action is "index" or "edit" or "delete" or ""))
        {
            return "all";
        }

        if (path.EndsWith("/admissions", StringComparison.Ordinal)
            || path.EndsWith("/admissions/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
