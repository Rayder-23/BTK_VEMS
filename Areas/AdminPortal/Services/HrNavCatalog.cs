namespace VEMS.Areas.AdminPortal.Services;

public sealed class HrSidebarNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class HrNavCatalog
{
    public static IReadOnlyList<HrSidebarNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/hr", IconClass = "fa-gauge-high" },
        new() { Key = "employees", Name = "Employees", Url = "/adminportal/hr/employees", IconClass = "fa-id-badge" },
        new() { Key = "leaves", Name = "Leaves", Url = "/adminportal/hr/leaves", IconClass = "fa-plane" },
        new() { Key = "attendance", Name = "Attendance", Url = "/adminportal/hr/attendance", IconClass = "fa-calendar-check" },
        new() { Key = "payroll", Name = "Payroll", Url = "/adminportal/hr/payroll", IconClass = "fa-money-check-dollar" },
        new() { Key = "tax", Name = "Tax", Url = "/adminportal/hr/tax", IconClass = "fa-percent" },
        new() { Key = "allowances", Name = "Allowances", Url = "/adminportal/hr/allowances", IconClass = "fa-circle-plus" },
        new() { Key = "deductions", Name = "Deductions", Url = "/adminportal/hr/deductions", IconClass = "fa-circle-minus" }
    ];

    private static readonly HashSet<string> HrControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "HR",
        "Employees",
        "Leaves",
        "HrAttendance",
        "Payroll",
        "Tax",
        "Allowances",
        "Deductions"
    };

    public static bool IsHrController(string controller) => HrControllers.Contains(controller);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        _ = action;

        if (path.Contains("/employees", StringComparison.Ordinal))
        {
            return "employees";
        }

        if (path.Contains("/leaves", StringComparison.Ordinal))
        {
            return "leaves";
        }

        if (path.Contains("/attendance", StringComparison.Ordinal))
        {
            return "attendance";
        }

        if (path.Contains("/payroll", StringComparison.Ordinal))
        {
            return "payroll";
        }

        if (path.Contains("/tax", StringComparison.Ordinal))
        {
            return "tax";
        }

        if (path.Contains("/allowances", StringComparison.Ordinal))
        {
            return "allowances";
        }

        if (path.Contains("/deductions", StringComparison.Ordinal))
        {
            return "deductions";
        }

        if (path.EndsWith("/hr", StringComparison.Ordinal) || path.EndsWith("/hr/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
