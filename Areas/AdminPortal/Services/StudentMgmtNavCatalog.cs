namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentMgmtNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public sealed class StudentMgmtNavModule
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string IconClass { get; init; }
    public required string DefaultUrl { get; init; }
    public required IReadOnlyList<StudentMgmtNavItem> Links { get; init; }
}

public static class StudentMgmtNavCatalog
{
    public static IReadOnlyList<StudentMgmtNavModule> TopBarModules { get; } =
    [
        new()
        {
            Key = "settings",
            Name = "Settings",
            IconClass = "fa-gear",
            DefaultUrl = "/adminportal/students/programs",
            Links =
            [
                new() { Key = "programs", Name = "Programs", Url = "/adminportal/students/programs", IconClass = "fa-graduation-cap" },
                new() { Key = "courses", Name = "Courses", Url = "/adminportal/students/courses", IconClass = "fa-book" },
                new() { Key = "classes", Name = "Classes", Url = "/adminportal/students/classes", IconClass = "fa-chalkboard" },
                new() { Key = "class-courses", Name = "Link-Class-Courses", Url = "/adminportal/students/class-courses", IconClass = "fa-layer-group" },
                new() { Key = "program-enrollments", Name = "Program Enrollment", Url = "/adminportal/students/program-enrollments", IconClass = "fa-id-card" },
                new() { Key = "course-enrollments", Name = "Course Enrolment", Url = "/adminportal/students/course-enrollments", IconClass = "fa-user-check" },
                new() { Key = "login", Name = "Create Login", Url = "/adminportal/students/login", IconClass = "fa-key" }
            ]
        },
        new()
        {
            Key = "fee",
            Name = "Fee",
            IconClass = "fa-coins",
            DefaultUrl = "/adminportal/students/fee",
            Links =
            [
                new() { Key = "fee", Name = "Fee", Url = "/adminportal/students/fee", IconClass = "fa-coins" },
                new() { Key = "challans", Name = "Challans", Url = "/adminportal/students/challans", IconClass = "fa-file-invoice-dollar" }
            ]
        },
        new()
        {
            Key = "main-links",
            Name = "Main Links",
            IconClass = "fa-ellipsis",
            DefaultUrl = "/adminportal/students/students",
            Links =
            [
                new() { Key = "students", Name = "Students", Url = "/adminportal/students/students", IconClass = "fa-users" },
                new() { Key = "attendance", Name = "Attendance", Url = "/adminportal/students/attendance", IconClass = "fa-calendar-check" },
                new() { Key = "results", Name = "Results", Url = "/adminportal/students/results", IconClass = "fa-clipboard-list" }
            ]
        }
    ];

    public static IReadOnlyList<StudentMgmtNavItem> SidebarPlaceholderNav { get; } =
    [
        new() { Key = "placeholder-1", Name = "", Url = "#", IconClass = "" }
    ];

    public static IReadOnlyList<StudentMgmtNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/students", IconClass = "fa-gauge-high" },
        new() { Key = "overview", Name = "Overview", Url = "/adminportal/students/dashboard", IconClass = "fa-chart-line" }
    ];

    private static readonly HashSet<string> StudentMgmtControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "StudentMgmt",
        "StudentDashboard",
        "Students",
        "ProgramEnrollments",
        "StudentLogin",
        "StudentCourses",
        "Classes",
        "ClassCourses",
        "StudentCourseEnrollments",
        "StudentPrograms",
        "StudentAttendance",
        "StudentResults",
        "StudentFee",
        "StudentChallans"
    };

    public static bool IsStudentMgmtController(string controller) => StudentMgmtControllers.Contains(controller);

    public static StudentMgmtNavModule? GetModule(string moduleKey) =>
        TopBarModules.FirstOrDefault(module =>
            string.Equals(module.Key, moduleKey, StringComparison.OrdinalIgnoreCase));

    public static string? ResolveModuleKey(string path, string action)
    {
        var activeKey = ResolveActiveKey(path, action);
        return TopBarModules.FirstOrDefault(module =>
            module.Links.Any(link => string.Equals(link.Key, activeKey, StringComparison.OrdinalIgnoreCase)))?.Key;
    }

    public static StudentMgmtNavModule? GetModuleForRoute(string path, string action)
    {
        var moduleKey = ResolveModuleKey(path, action);
        return moduleKey is null ? null : GetModule(moduleKey);
    }

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/students/students", StringComparison.Ordinal)
            || string.Equals(path, "/adminportal/students/students", StringComparison.Ordinal))
        {
            return "students";
        }

        if (path.Contains("/program-enrollments", StringComparison.Ordinal))
        {
            return "program-enrollments";
        }

        if (path.Contains("/login", StringComparison.Ordinal))
        {
            return "login";
        }

        if (path.Contains("/class-courses", StringComparison.Ordinal))
        {
            return "class-courses";
        }

        if (path.Contains("/classes", StringComparison.Ordinal))
        {
            return "classes";
        }

        if (path.Contains("/course-enrollments", StringComparison.Ordinal))
        {
            return "course-enrollments";
        }

        if (path.Contains("/courses", StringComparison.Ordinal))
        {
            return "courses";
        }

        if (path.Contains("/programs", StringComparison.Ordinal))
        {
            return "programs";
        }

        if (path.Contains("/attendance", StringComparison.Ordinal))
        {
            return "attendance";
        }

        if (path.Contains("/results", StringComparison.Ordinal))
        {
            return "results";
        }

        if (path.Contains("/fee", StringComparison.Ordinal) && !path.Contains("/challans", StringComparison.Ordinal))
        {
            return "fee";
        }

        if (path.Contains("/challans", StringComparison.Ordinal))
        {
            return "challans";
        }

        if (path.Contains("/dashboard", StringComparison.Ordinal))
        {
            return "overview";
        }

        if (path.EndsWith("/students", StringComparison.Ordinal)
            || path.EndsWith("/students/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
