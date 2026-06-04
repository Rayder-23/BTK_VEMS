namespace VEMS.Areas.AdminPortal.Services;

public sealed class StudentMgmtNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class StudentMgmtNavCatalog
{
    public static IReadOnlyList<StudentMgmtNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/students", IconClass = "fa-gauge-high" },
        new() { Key = "overview", Name = "Overview", Url = "/adminportal/students/dashboard", IconClass = "fa-chart-line" },
        new() { Key = "students", Name = "Students", Url = "/adminportal/students/students", IconClass = "fa-users" },
        new() { Key = "login", Name = "Create Login", Url = "/adminportal/students/login", IconClass = "fa-key" },
        new() { Key = "courses", Name = "Courses", Url = "/adminportal/students/courses", IconClass = "fa-book" },
        new() { Key = "attendance", Name = "Attendance", Url = "/adminportal/students/attendance", IconClass = "fa-calendar-check" },
        new() { Key = "results", Name = "Results", Url = "/adminportal/students/results", IconClass = "fa-clipboard-list" },
        new() { Key = "fee", Name = "Fee", Url = "/adminportal/students/fee", IconClass = "fa-coins" },
        new() { Key = "challans", Name = "Challans", Url = "/adminportal/students/challans", IconClass = "fa-file-invoice-dollar" }
    ];

    private static readonly HashSet<string> StudentMgmtControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "StudentMgmt",
        "StudentDashboard",
        "Students",
        "StudentLogin",
        "StudentCourses",
        "StudentAttendance",
        "StudentResults",
        "StudentFee",
        "StudentChallans"
    };

    public static bool IsStudentMgmtController(string controller) => StudentMgmtControllers.Contains(controller);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/students/students", StringComparison.Ordinal)
            || string.Equals(path, "/adminportal/students/students", StringComparison.Ordinal))
        {
            return "students";
        }

        if (path.Contains("/login", StringComparison.Ordinal))
        {
            return "login";
        }

        if (path.Contains("/courses", StringComparison.Ordinal))
        {
            return "courses";
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
