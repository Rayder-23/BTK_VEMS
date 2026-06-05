using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public static class AdminModuleCatalog
{
    public static IReadOnlyList<AdminModuleCard> Modules { get; } =
    [
        new()
        {
            Name = "Admissions",
            Controller = "AdmissionsMgmt",
            IconClass = "bi-door-open-fill",
            Description = "Student applications, tests, payments, and enrollment conversion",
            AccentClass = "accent-cyan",
            ImageUrl = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true,
            UrlOverride = "/adminportal/admissions"
        },
        new()
        {
            Name = "Students Management",
            Controller = "StudentMgmt",
            IconClass = "bi-mortarboard-fill",
            Description = "Students, courses, attendance, results, fees, challans, and logins",
            AccentClass = "accent-blue",
            ImageUrl = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=800&q=80&auto=format&fit=crop",
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
            ImageUrl = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true,
            UrlOverride = "/adminportal/fee"
        },
        new()
        {
            Name = "Settings",
            Controller = "Settings",
            IconClass = "bi-gear-fill",
            Description = "Configure system-wide options",
            AccentClass = "accent-indigo",
            ImageUrl = "https://images.unsplash.com/photo-1454165804603-c3d57bc86b40?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true
        },
        new()
        {
            Name = "Accounts",
            Controller = "Accounts",
            IconClass = "bi-bank2",
            Description = "Manage finance and ledger accounts",
            AccentClass = "accent-pink",
            ImageUrl = "https://images.unsplash.com/photo-1554224154-26032ffc0d07?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true
        },
        new()
        {
            Name = "HR Management",
            Controller = "HR",
            IconClass = "bi-diagram-3-fill",
            Description = "Employees, payroll, leaves, attendance, tax, and more",
            AccentClass = "accent-teal",
            ImageUrl = "https://images.unsplash.com/photo-1521737711864-e4b37134e9ee?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true,
            UrlOverride = "/adminportal/hr"
        },
        new()
        {
            Name = "Examination",
            Controller = "ExaminationMgmt",
            IconClass = "bi-pencil-square",
            Description = "Exam types, marking, schedules, grades, and semester results",
            AccentClass = "accent-green",
            ImageUrl = "https://images.unsplash.com/photo-1434030214721-40b911d68f07?w=800&q=80&auto=format&fit=crop",
            IsAvailable = true,
            UrlOverride = "/adminportal/examination"
        },
        new()
        {
            Name = "Library",
            Controller = "Library",
            IconClass = "bi-book-fill",
            Description = "Library catalog and circulation",
            AccentClass = "accent-purple",
            ImageUrl = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        },
        new()
        {
            Name = "Transport",
            Controller = "Transport",
            IconClass = "bi-bus-front-fill",
            Description = "Routes, vehicles, and transport fees",
            AccentClass = "accent-orange",
            ImageUrl = "https://images.unsplash.com/photo-1544625617-1a401a8c2d4e?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        },
        new()
        {
            Name = "Hostel",
            Controller = "Hostel",
            IconClass = "bi-house-door-fill",
            Description = "Hostel allocation and facilities",
            AccentClass = "accent-red",
            ImageUrl = "https://images.unsplash.com/photo-1555854877-bab0efeef178?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        },
        new()
        {
            Name = "Notifications",
            Controller = "Notifications",
            IconClass = "bi-bell-fill",
            Description = "Alerts and announcements",
            AccentClass = "accent-teal",
            ImageUrl = "https://images.unsplash.com/photo-1516321318423-f06f868d685f?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        },
        new()
        {
            Name = "Reports",
            Controller = "Reports",
            IconClass = "bi-pie-chart-fill",
            Description = "Analytics and operational reports",
            AccentClass = "accent-indigo",
            ImageUrl = "https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        },
        new()
        {
            Name = "Timetable",
            Controller = "Timetable",
            IconClass = "bi-clock-fill",
            Description = "Class schedules and room planning",
            AccentClass = "accent-pink",
            ImageUrl = "https://images.unsplash.com/photo-1506784365847-bbad939e9335?w=800&q=80&auto=format&fit=crop",
            IsAvailable = false
        }
    ];

    public static IReadOnlyList<AdminModuleCard> NavigableModules { get; } =
        Modules.Where(module => module.IsAvailable).ToList();

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
        IReadOnlyList<IReadOnlyList<string>> rows) =>
        new()
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
