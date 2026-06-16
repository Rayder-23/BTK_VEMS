namespace VEMS.Areas.AdminPortal.Services;

public sealed class TimetableNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class TimetableNavCatalog
{
    public static IReadOnlyList<TimetableNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Class timetable", Url = "/adminportal/timetable", IconClass = "fa-calendar-week" },
        new() { Key = "periods", Name = "Periods", Url = "/adminportal/settings/periods", IconClass = "fa-clock" },
        new() { Key = "timetable-slots", Name = "Timetable slots", Url = "/adminportal/settings/timetables", IconClass = "fa-table-list" },
        new() { Key = "teacher-assignments", Name = "Teacher assignments", Url = "/adminportal/teachers/link-teacher-assignment", IconClass = "fa-link" }
    ];

    public static bool IsTimetableController(string controller) =>
        string.Equals(controller, "Timetable", StringComparison.OrdinalIgnoreCase);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        _ = action;

        if (path.Contains("/settings/periods", StringComparison.Ordinal))
        {
            return "periods";
        }

        if (path.Contains("/settings/timetables", StringComparison.Ordinal))
        {
            return "timetable-slots";
        }

        if (path.Contains("/teachers/link-teacher-assignment", StringComparison.Ordinal))
        {
            return "teacher-assignments";
        }

        if (path.EndsWith("/timetable", StringComparison.Ordinal)
            || path.EndsWith("/timetable/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
