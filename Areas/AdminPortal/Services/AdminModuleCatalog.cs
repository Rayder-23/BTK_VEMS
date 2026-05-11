using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public static class AdminModuleCatalog
{
    public static IReadOnlyList<AdminModuleCard> Modules { get; } =
    [
        new() { Name = "Students", Controller = "Students", IconClass = "bi-people-fill", Description = "Manage student records", AccentClass = "accent-blue" },
        new() { Name = "Courses", Controller = "Courses", IconClass = "bi-journal-bookmark-fill", Description = "Manage courses and programs", AccentClass = "accent-green" },
        new() { Name = "Attendance", Controller = "Attendance", IconClass = "bi-calendar-check-fill", Description = "Track daily attendance", AccentClass = "accent-purple" },
        new() { Name = "Results", Controller = "Results", IconClass = "bi-clipboard-data-fill", Description = "Manage exams and results", AccentClass = "accent-orange" },
        new() { Name = "Fee", Controller = "Fee", IconClass = "bi-cash-coin", Description = "Manage fee structures", AccentClass = "accent-red" },
        new() { Name = "Challans", Controller = "Challans", IconClass = "bi-receipt-cutoff", Description = "Generate and track challans", AccentClass = "accent-blue" },
        new() { Name = "Settings", Controller = "Settings", IconClass = "bi-gear-fill", Description = "Configure system options", AccentClass = "accent-purple" },
        new() { Name = "Employees", Controller = "Employees", IconClass = "bi-person-badge-fill", Description = "Manage employee records", AccentClass = "accent-green" },
        new() { Name = "Accounts", Controller = "Accounts", IconClass = "bi-bank2", Description = "Manage finance accounts", AccentClass = "accent-orange" },
        new() { Name = "HR", Controller = "HR", IconClass = "bi-diagram-3-fill", Description = "Manage HR operations", AccentClass = "accent-red" }
    ];

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
            "Employees" => CreatePage(module, "Add Employee", ["Employee ID", "Name", "Department", "Status"],
            [
                ["EMP-2001", "Sara Ahmed", "Academics", "Active"],
                ["EMP-2002", "Usman Raza", "Finance", "Active"],
                ["EMP-2003", "Mariam Iqbal", "Administration", "On Leave"]
            ]),
            "Accounts" => CreatePage(module, "Add Account", ["Account", "Category", "Balance", "Status"],
            [
                ["Main Cash", "Asset", "250,000", "Active"],
                ["Tuition Revenue", "Income", "1,250,000", "Active"],
                ["Scholarship Fund", "Reserve", "300,000", "Active"]
            ]),
            "HR" => CreatePage(module, "Add HR Record", ["Record ID", "Employee", "Type", "Status"],
            [
                ["HR-001", "Sara Ahmed", "Contract", "Approved"],
                ["HR-002", "Usman Raza", "Leave Request", "Pending"],
                ["HR-003", "Mariam Iqbal", "Performance", "Review"]
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
