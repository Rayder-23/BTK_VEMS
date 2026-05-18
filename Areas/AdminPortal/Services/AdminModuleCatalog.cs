using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public static class AdminModuleCatalog
{
    public static IReadOnlyList<AdminModuleCard> Modules { get; } =
    [
        new()
        {
            Name = "Students Management",
            Controller = "StudentMgmt",
            IconClass = "bi-mortarboard-fill",
            Description = "Students, courses, attendance, results, fees, challans, and logins",
            AccentClass = "accent-blue",
            IsAvailable = true,
            UrlOverride = "/adminportal/students"
        },
        new()
        {
            Name = "Fee Management",
            Controller = "FeeMgmt",
            IconClass = "bi-cash-coin",
            Description = "Fee heads, structures, challans, payments, receipts, and concessions",
            AccentClass = "accent-red",
            IsAvailable = true,
            UrlOverride = "/adminportal/fee"
        },
        new() { Name = "Settings", Controller = "Settings", IconClass = "bi-gear-fill", Description = "Configure system-wide options", AccentClass = "accent-indigo", IsAvailable = true },
        new() { Name = "Accounts", Controller = "Accounts", IconClass = "bi-bank2", Description = "Manage finance and ledger accounts", AccentClass = "accent-pink", IsAvailable = true },
        new()
        {
            Name = "HR Management",
            Controller = "HR",
            IconClass = "bi-diagram-3-fill",
            Description = "Employees, payroll, leaves, attendance, tax, and more",
            AccentClass = "accent-teal",
            IsAvailable = true,
            UrlOverride = "/adminportal/hr"
        },
        new() { Name = "Examination", Controller = "Examination", IconClass = "bi-pencil-square", Description = "Exam scheduling and assessment", AccentClass = "accent-green", IsAvailable = false },
        new() { Name = "Library", Controller = "Library", IconClass = "bi-book-fill", Description = "Library catalog and circulation", AccentClass = "accent-purple", IsAvailable = false },
        new() { Name = "Transport", Controller = "Transport", IconClass = "bi-bus-front-fill", Description = "Routes, vehicles, and transport fees", AccentClass = "accent-orange", IsAvailable = false },
        new() { Name = "Hostel", Controller = "Hostel", IconClass = "bi-house-door-fill", Description = "Hostel allocation and facilities", AccentClass = "accent-red", IsAvailable = false },
        new() { Name = "Notifications", Controller = "Notifications", IconClass = "bi-bell-fill", Description = "Alerts and announcements", AccentClass = "accent-teal", IsAvailable = false },
        new() { Name = "Reports", Controller = "Reports", IconClass = "bi-pie-chart-fill", Description = "Analytics and operational reports", AccentClass = "accent-indigo", IsAvailable = false },
        new() { Name = "Admissions", Controller = "Admissions", IconClass = "bi-door-open-fill", Description = "Admission inquiries and enrollment", AccentClass = "accent-cyan", IsAvailable = false },
        new() { Name = "Timetable", Controller = "Timetable", IconClass = "bi-clock-fill", Description = "Class schedules and room planning", AccentClass = "accent-pink", IsAvailable = false }
    ];

    public static IReadOnlyList<AdminModuleCard> NavigableModules { get; } =
        Modules.Where(module => module.IsAvailable).ToList();

    public static IReadOnlyList<AdminStatisticCard> Statistics { get; } =
    [
        new() { Title = "Total Students", Value = "1,240", IconClass = "bi-people-fill", AccentClass = "accent-blue" },
        new() { Title = "Total Teachers", Value = "86", IconClass = "bi-person-video3", AccentClass = "accent-green" },
        new() { Title = "Total Courses", Value = "42", IconClass = "bi-journal-bookmark-fill", AccentClass = "accent-purple" },
        new() { Title = "Total Employees", Value = "128", IconClass = "bi-person-badge-fill", AccentClass = "accent-orange" }
    ];

    public static AdminModulePageViewModel CreateModulePage(string controller)
    {
        var module = Modules.First(item => item.Controller == controller);

        return controller switch
        {
            "Settings" => CreatePage(module, "Add Setting", ["Setting", "Value", "Scope", "Status"],
            [
                ["Academic Year", "2026", "Global", "Active"],
                ["Attendance Lock", "Enabled", "Portal", "Active"],
                ["Result Approval", "Required", "Exam", "Active"]
            ]),
            "Accounts" => CreatePage(module, "Add Account", ["Account", "Category", "Balance", "Status"],
            [
                ["Main Cash", "Asset", "250,000", "Active"],
                ["Tuition Revenue", "Income", "1,250,000", "Active"],
                ["Scholarship Fund", "Reserve", "300,000", "Active"]
            ]),
            _ => throw new ArgumentOutOfRangeException(nameof(controller), controller, "Unknown admin module.")
        };
    }

    private static AdminModulePageViewModel CreatePage(
        AdminModuleCard module,
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
            TableRows = rows
        };
    }
}
