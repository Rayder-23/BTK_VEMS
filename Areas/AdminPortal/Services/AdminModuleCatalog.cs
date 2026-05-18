using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public static class AdminModuleCatalog
{
    public static IReadOnlyList<AdminModuleCard> Modules { get; } =
    [
        new() { Name = "Students", Controller = "Students", IconClass = "bi-people-fill", Description = "Manage student records and profiles", AccentClass = "accent-blue", IsAvailable = true },
        new() { Name = "Courses", Controller = "Courses", IconClass = "bi-journal-bookmark-fill", Description = "Manage courses and academic programs", AccentClass = "accent-green", IsAvailable = true },
        new() { Name = "Attendance", Controller = "Attendance", IconClass = "bi-calendar-check-fill", Description = "Track daily attendance records", AccentClass = "accent-purple", IsAvailable = true },
        new() { Name = "Results", Controller = "Results", IconClass = "bi-clipboard-data-fill", Description = "Manage exams and student results", AccentClass = "accent-orange", IsAvailable = true },
        new() { Name = "Fee", Controller = "Fee", IconClass = "bi-cash-coin", Description = "Configure fee structures and billing", AccentClass = "accent-red", IsAvailable = true },
        new() { Name = "Challan", Controller = "Challans", IconClass = "bi-receipt-cutoff", Description = "Generate and track fee challans", AccentClass = "accent-teal", IsAvailable = true },
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
            "Students" => CreatePage(module, "Add Student", ["Student ID", "Name", "Class", "Status"],
            [
                ["STD-1001", "Ayesha Khan", "BS Computer Science", "Active"],
                ["STD-1002", "Hamza Ali", "BBA", "Active"],
                ["STD-1003", "Fatima Noor", "Intermediate", "Pending"]
            ]),
            "Courses" => CreatePage(module, "Add Course", ["Code", "Course Name", "Credit Hours", "Status"],
            [
                ["CS-101", "Introduction to Programming", "3", "Active"],
                ["MTH-210", "Linear Algebra", "3", "Active"],
                ["ENG-115", "Communication Skills", "2", "Draft"]
            ]),
            "Attendance" => CreatePage(module, "Add Attendance", ["Date", "Class", "Present", "Absent"],
            [
                ["2026-05-11", "BSCS Semester 4", "42", "3"],
                ["2026-05-11", "BBA Semester 2", "37", "5"],
                ["2026-05-10", "Intermediate", "58", "6"]
            ]),
            "Results" => CreatePage(module, "Add Result", ["Exam", "Class", "Published", "Status"],
            [
                ["Mid Term", "BSCS Semester 4", "Yes", "Published"],
                ["Quiz 2", "BBA Semester 2", "No", "Review"],
                ["Final Term", "Intermediate", "No", "Draft"]
            ]),
            "Fee" => CreatePage(module, "Add Fee", ["Fee Type", "Program", "Amount", "Status"],
            [
                ["Tuition Fee", "BSCS", "45,000", "Active"],
                ["Library Fee", "All Programs", "2,500", "Active"],
                ["Lab Fee", "Computer Science", "5,000", "Active"]
            ]),
            "Challans" => CreatePage(module, "Generate Challan", ["Challan No", "Student", "Amount", "Status"],
            [
                ["CH-2026-001", "Ayesha Khan", "45,000", "Paid"],
                ["CH-2026-002", "Hamza Ali", "42,000", "Pending"],
                ["CH-2026-003", "Fatima Noor", "38,000", "Overdue"]
            ]),
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
