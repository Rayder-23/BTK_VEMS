using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentMgmtSubNavItem
{
    public required string Name { get; init; }

    public required string Action { get; init; }

    public string BuildUrl(string segment) =>
        string.Equals(Action, "Index", StringComparison.OrdinalIgnoreCase)
            ? $"/adminportal/students/{segment}"
            : $"/adminportal/students/{segment}/{Action.ToLowerInvariant()}";
}

public sealed class StudentMgmtModule
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Controller { get; init; }

    public required string Segment { get; init; }

    public required string IconClass { get; init; }

    public required string Description { get; init; }

    public required string AccentClass { get; init; }

    public required IReadOnlyList<StudentMgmtSubNavItem> SubNav { get; init; }

    public string Url => string.Equals(Controller, "Teachers", StringComparison.OrdinalIgnoreCase)
        ? "/adminportal/teachers"
        : $"/adminportal/students/{Segment}";
}

public static class StudentMgmtModuleCatalog
{
    public static IReadOnlyList<StudentMgmtModule> GridModules { get; } =
    [
        Module("Dashboard", "StudentDashboard", "dashboard", "bi-speedometer2", "Student management overview", "accent-blue",
            Sub("Overview", "Index"), Sub("Reports", "Reports")),
        Module("Students", "Students", "students", "bi-people-fill", "Student records and profiles", "accent-cyan",
            Sub("Add Student", "Create"), Sub("All Students", "Index"), Sub("Previous Students", "Previous")),
        Module("ProgramEnrollments", "ProgramEnrollments", "program-enrollments", "bi-person-lines-fill", "Student program enrollments per academic period", "accent-cyan",
            Sub("Add Enrollment", "Create"), Sub("All Enrollments", "Index")),
        Module("CreateLogin", "StudentLogin", "login", "bi-key-fill", "Portal login accounts for students", "accent-indigo",
            Sub("Add Login", "Create"), Sub("All Logins", "Index")),
        Module("Courses", "StudentCourses", "courses", "bi-journal-bookmark-fill", "Courses and academic programs", "accent-green",
            Sub("Add Course", "Create"), Sub("All Courses", "Index")),
        Module("Classes", "Classes", "classes", "bi-door-open-fill", "Teaching classes and cohorts", "accent-purple",
            Sub("Add Class", "Create"), Sub("All Classes", "Index")),
        Module("ClassCourses", "ClassCourses", "class-courses", "bi-collection", "Link courses to teaching classes", "accent-teal",
            Sub("Assign Course", "Create"), Sub("All Class Courses", "Index")),
        Module("CourseEnrollments", "StudentCourseEnrollments", "course-enrollments", "bi-person-check-fill", "Enroll students in class courses", "accent-orange",
            Sub("Enroll Student", "Create"), Sub("All Enrollments", "Index")),
        Module("Programs", "StudentPrograms", "programs", "bi-mortarboard-fill", "Academic programs catalog", "accent-pink",
            Sub("Add Program", "Create"), Sub("All Programs", "Index"), Sub("Program Courses", "Courses")),
        Module("Teachers", "Teachers", "teachers", "bi-person-workspace", "Faculty records and class or course assignments", "accent-indigo",
            Sub("Add Teacher", "Create"), Sub("All Teachers", "Index")),
        Module("Attendance", "StudentAttendance", "attendance", "bi-calendar-check-fill", "Daily attendance records", "accent-purple",
            Sub("Mark Attendance", "Create"), Sub("All Records", "Index")),
        Module("Results", "StudentResults", "results", "bi-clipboard-data-fill", "Exams and student results", "accent-orange",
            Sub("Add Result", "Create"), Sub("All Results", "Index")),
        Module("Fee", "StudentFee", "fee", "bi-cash-coin", "Fee structures and billing", "accent-red",
            Sub("Add Fee", "Create"), Sub("All Fees", "Index")),
        Module("Challan", "StudentChallans", "challans", "bi-receipt-cutoff", "Fee challans and payments", "accent-teal",
            Sub("Generate Challan", "Create"), Sub("All Challans", "Index"))
    ];

    public static StudentMgmtModule Get(string key) =>
        GridModules.First(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    public static StudentMgmtModule? TryGetByController(string controller) =>
        GridModules.FirstOrDefault(m =>
            string.Equals(m.Controller, controller, StringComparison.OrdinalIgnoreCase));

    public static AdminModulePageViewModel CreateListPage(string key)
    {
        var module = Get(key);
        return key switch
        {
            "Courses" => CreatePage(module, "Add Course",
                ["Code", "Course Name", "Credit Hours", "Status"],
                [
                    ["CS-101", "Introduction to Programming", "3", "Active"],
                    ["MTH-210", "Linear Algebra", "3", "Active"],
                    ["ENG-115", "Communication Skills", "2", "Draft"]
                ]),
            "Attendance" => CreatePage(module, "Mark Attendance",
                ["Date", "Class", "Present", "Absent"],
                [
                    ["2026-05-18", "BSCS Semester 4", "42", "3"],
                    ["2026-05-18", "BBA Semester 2", "37", "5"],
                    ["2026-05-17", "Intermediate", "58", "6"]
                ]),
            "Results" => CreatePage(module, "Add Result",
                ["Exam", "Class", "Published", "Status"],
                [
                    ["Mid Term", "BSCS Semester 4", "Yes", "Published"],
                    ["Quiz 2", "BBA Semester 2", "No", "Review"],
                    ["Final Term", "Intermediate", "No", "Draft"]
                ]),
            "Fee" => CreatePage(module, "Add Fee",
                ["Fee Type", "Program", "Amount", "Status"],
                [
                    ["Tuition Fee", "BSCS", "45,000", "Active"],
                    ["Library Fee", "All Programs", "2,500", "Active"],
                    ["Lab Fee", "Computer Science", "5,000", "Active"]
                ]),
            "Challan" => CreatePage(module, "Generate Challan",
                ["Challan No", "Student", "Amount", "Status"],
                [
                    ["CH-2026-001", "Ayesha Khan", "45,000", "Paid"],
                    ["CH-2026-002", "Hamza Ali", "42,000", "Pending"],
                    ["CH-2026-003", "Fatima Noor", "38,000", "Overdue"]
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown student management module.")
        };
    }

    public static StudentMgmtFormPlaceholderViewModel CreateFormPage(string key, int? id = null)
    {
        var module = Get(key);
        return new StudentMgmtFormPlaceholderViewModel
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

    private static StudentMgmtModule Module(
        string key,
        string controller,
        string segment,
        string icon,
        string description,
        string accent,
        params StudentMgmtSubNavItem[] subNav) =>
        new()
        {
            Key = key,
            Name = key switch
            {
                "CreateLogin" => "Create Login",
                "ClassCourses" => "Class Courses",
                "ProgramEnrollments" => "Program Enrollment",
                "CourseEnrollments" => "Course Enrollments",
                "Classes" => "Classes",
                "Challan" => "Challan",
                _ => key
            },
            Controller = controller,
            Segment = segment,
            IconClass = icon,
            Description = description,
            AccentClass = accent,
            SubNav = subNav
        };

    private static StudentMgmtSubNavItem Sub(string name, string action) =>
        new() { Name = name, Action = action };

    private static AdminModulePageViewModel CreatePage(
        StudentMgmtModule module,
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
