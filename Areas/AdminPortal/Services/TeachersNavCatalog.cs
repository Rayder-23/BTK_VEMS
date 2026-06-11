namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeachersSidebarNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class TeachersNavCatalog
{
    public static IReadOnlyList<TeachersSidebarNavItem> SidebarLeadingPlaceholderNav { get; } =
    [
        new() { Key = "placeholder-top", Name = "", Url = "#", IconClass = "" }
    ];

    public static IReadOnlyList<TeachersSidebarNavItem> SidebarSubNav { get; } =
    [
        new() { Key = "add-teachers", Name = "Add Teachers", Url = "/adminportal/teachers/create", IconClass = "fa-user-plus" },
        new() { Key = "all-teachers", Name = "All Teachers", Url = "/adminportal/teachers/all", IconClass = "fa-chalkboard-user" },
        new() { Key = "teacher-class-courses", Name = "Teachers-Class-Course", Url = "/adminportal/teachers/teacher-class-courses", IconClass = "fa-link" }
    ];

    public static IReadOnlyList<TeachersSidebarNavItem> SidebarPlaceholderNav { get; } =
    [
        new() { Key = "placeholder-1", Name = "", Url = "#", IconClass = "" }
    ];

    public static IReadOnlyList<TeachersSidebarNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/teachers", IconClass = "fa-gauge-high" },
        new() { Key = "overview", Name = "Overview", Url = "/adminportal/teachers/dashboard", IconClass = "fa-chart-line" }
    ];

    private static readonly HashSet<string> TeachersControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "TeachersMgmt",
        "TeacherDashboard",
        "Teachers",
        "TeacherClassCourses"
    };

    public static bool IsTeachersController(string? controller) =>
        !string.IsNullOrEmpty(controller) && TeachersControllers.Contains(controller);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        _ = action;

        if (path.Contains("/teacher-class-courses", StringComparison.Ordinal))
        {
            return "teacher-class-courses";
        }

        if (path.Contains("/dashboard", StringComparison.Ordinal))
        {
            return "overview";
        }

        if (path.Contains("/teachers/create", StringComparison.Ordinal))
        {
            return "add-teachers";
        }

        if (path.Contains("/teachers/all", StringComparison.Ordinal))
        {
            return "all-teachers";
        }

        if (string.Equals(path, "/adminportal/teachers", StringComparison.Ordinal)
            || path.EndsWith("/teachers/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        if (path.Contains("/teachers", StringComparison.Ordinal))
        {
            return "all-teachers";
        }

        return "dashboard";
    }
}
