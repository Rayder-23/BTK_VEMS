namespace VEMS.Areas.AdminPortal.Services;

public sealed class SettingsNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class SettingsNavCatalog
{
    public static IReadOnlyList<SettingsNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/settings", IconClass = "fa-gauge-high" },
        new() { Key = "add", Name = "Add Configuration", Url = "/adminportal/settings/create", IconClass = "fa-plus" }
    ];

    public static bool IsSettingsController(string controller) =>
        string.Equals(controller, "Settings", StringComparison.OrdinalIgnoreCase);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/create", StringComparison.Ordinal))
        {
            return "add";
        }

        if (path.EndsWith("/settings", StringComparison.Ordinal) || path.EndsWith("/settings/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
