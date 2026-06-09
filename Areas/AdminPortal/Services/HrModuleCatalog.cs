using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class HrNavItem
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Controller { get; init; }

    public required string Segment { get; init; }

    public required string IconClass { get; init; }

    public required string Description { get; init; }

    public required string AccentClass { get; init; }

    public string Url => string.IsNullOrEmpty(Segment)
        ? "/adminportal/hr"
        : $"/adminportal/hr/{Segment}";
}

public static class HrModuleCatalog
{
    public static IReadOnlyList<HrNavItem> NavItems { get; } =
    [
        new()
        {
            Key = "Home",
            Name = "HR Home",
            Controller = "HR",
            Segment = "",
            IconClass = "bi-house-door-fill",
            Description = "Human resources overview and quick links",
            AccentClass = "accent-teal"
        },
        new()
        {
            Key = "Employees",
            Name = "Employees",
            Controller = "Employees",
            Segment = "employees",
            IconClass = "bi-person-badge-fill",
            Description = "Employee records and profiles",
            AccentClass = "accent-cyan"
        },
        new()
        {
            Key = "EmployeeLogin",
            Name = "Create Login",
            Controller = "EmployeeLogin",
            Segment = "employee-login",
            IconClass = "bi-key-fill",
            Description = "Employee portal login accounts",
            AccentClass = "accent-indigo"
        },
        new()
        {
            Key = "Leaves",
            Name = "Leaves",
            Controller = "Leaves",
            Segment = "leaves",
            IconClass = "bi-airplane-fill",
            Description = "Leave requests, balances, and approvals",
            AccentClass = "accent-green"
        },
        new()
        {
            Key = "Attendance",
            Name = "Attendance",
            Controller = "HrAttendance",
            Segment = "attendance",
            IconClass = "bi-calendar-check-fill",
            Description = "Daily attendance and timesheets",
            AccentClass = "accent-purple"
        },
        new()
        {
            Key = "Payroll",
            Name = "Payroll",
            Controller = "Payroll",
            Segment = "payroll",
            IconClass = "bi-wallet2",
            Description = "Salary processing and payslips",
            AccentClass = "accent-blue"
        },
        new()
        {
            Key = "Tax",
            Name = "Tax",
            Controller = "Tax",
            Segment = "tax",
            IconClass = "bi-percent",
            Description = "Tax rules and employee tax profiles",
            AccentClass = "accent-orange"
        },
        new()
        {
            Key = "Allowances",
            Name = "Allowances",
            Controller = "Allowances",
            Segment = "allowances",
            IconClass = "bi-plus-circle-fill",
            Description = "Allowance types and employee allocations",
            AccentClass = "accent-indigo"
        },
        new()
        {
            Key = "Deductions",
            Name = "Deductions",
            Controller = "Deductions",
            Segment = "deductions",
            IconClass = "bi-dash-circle-fill",
            Description = "Deduction types and employee allocations",
            AccentClass = "accent-pink"
        }
    ];

    public static IReadOnlyList<HrNavItem> ModuleNavItems { get; } =
        NavItems.Where(item => !string.IsNullOrEmpty(item.Segment)).ToList();

    public static HrNavItem Get(string key) =>
        NavItems.First(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));

    public static AdminModulePageViewModel CreateListPage(string key)
    {
        var module = Get(key);
        return key switch
        {
            "Leaves" => CreatePage(module, "Add leave record",
                ["Request ID", "Employee", "Type", "Days", "Status"],
                [
                    ["LV-1001", "Sara Ahmed", "Annual", "3", "Approved"],
                    ["LV-1002", "Usman Raza", "Sick", "1", "Pending"],
                    ["LV-1003", "Mariam Iqbal", "Casual", "2", "Rejected"]
                ]),
            "Attendance" => CreatePage(module, "Add attendance",
                ["Date", "Employee", "Check-in", "Check-out", "Status"],
                [
                    ["2026-05-18", "Sara Ahmed", "09:02", "17:05", "Present"],
                    ["2026-05-18", "Usman Raza", "09:15", "17:00", "Late"],
                    ["2026-05-18", "Mariam Iqbal", "—", "—", "Absent"]
                ]),
            "Payroll" => CreatePage(module, "Add payroll run",
                ["Run ID", "Period", "Employees", "Net pay", "Status"],
                [
                    ["PR-2026-04", "Apr 2026", "128", "4,250,000", "Posted"],
                    ["PR-2026-05", "May 2026", "128", "4,310,000", "Draft"],
                    ["PR-2026-03", "Mar 2026", "127", "4,180,000", "Posted"]
                ]),
            "Tax" => CreatePage(module, "Add tax rule",
                ["Code", "Name", "Rate", "Applies to", "Status"],
                [
                    ["TAX-STD", "Standard slab", "Variable", "All staff", "Active"],
                    ["TAX-ADV", "Advance tax", "2%", "Contract", "Active"],
                    ["TAX-EXM", "Exemption", "0%", "Selected", "Active"]
                ]),
            "Allowances" => CreatePage(module, "Add allowance",
                ["Code", "Name", "Amount", "Frequency", "Status"],
                [
                    ["ALW-HRA", "House rent", "15,000", "Monthly", "Active"],
                    ["ALW-MED", "Medical", "5,000", "Monthly", "Active"],
                    ["ALW-TRN", "Transport", "3,500", "Monthly", "Active"]
                ]),
            "Deductions" => CreatePage(module, "Add deduction",
                ["Code", "Name", "Amount", "Frequency", "Status"],
                [
                    ["DED-LOAN", "Staff loan", "8,000", "Monthly", "Active"],
                    ["DED-ADV", "Salary advance", "10,000", "One-time", "Active"],
                    ["DED-LATE", "Late penalty", "500", "Monthly", "Active"]
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown HR module.")
        };
    }

    public static HrFormPlaceholderViewModel CreateFormPage(string key, int? id = null)
    {
        var module = Get(key);
        return new HrFormPlaceholderViewModel
        {
            ModuleName = module.Name,
            ModuleKey = module.Key,
            Segment = module.Segment,
            IconClass = module.IconClass,
            AccentClass = module.AccentClass,
            Description = module.Description,
            Id = id
        };
    }

    private static AdminModulePageViewModel CreatePage(
        HrNavItem module,
        string addButtonText,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        return new AdminModulePageViewModel
        {
            Title = module.Name,
            Description = module.Description,
            IconClass = module.IconClass,
            AccentClass = module.AccentClass,
            AddButtonText = addButtonText,
            TableHeaders = headers,
            TableRows = rows,
            ModuleSegment = module.Segment
        };
    }
}
