namespace VEMS.Areas.AdminPortal.Services;

public sealed class AccountsNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class AccountsNavCatalog
{
    public static IReadOnlyList<AccountsNavItem> SidebarNav { get; } =
    [
        new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/accounts", IconClass = "fa-gauge-high" }
    ];

    public static bool IsAccountsController(string controller) =>
        string.Equals(controller, "Accounts", StringComparison.OrdinalIgnoreCase);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        _ = action;

        if (path.EndsWith("/accounts", StringComparison.Ordinal) || path.EndsWith("/accounts/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
