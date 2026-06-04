namespace VEMS.Areas.AdminPortal.Services.Examination;

public sealed class ExaminationNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public static class ExaminationNavCatalog
{
    public static IReadOnlyList<ExaminationNavItem> SidebarNav { get; } = BuildSidebarNav();

    private static readonly HashSet<string> ExaminationControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExaminationMgmt"
    };

    static ExaminationNavCatalog()
    {
        foreach (var module in ExaminationModuleCatalog.SidebarModules)
        {
            ExaminationControllers.Add(module.Controller);
        }
    }

    public static bool IsExaminationController(string controller) =>
        ExaminationControllers.Contains(controller);

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        _ = action;

        foreach (var module in ExaminationModuleCatalog.SidebarModules)
        {
            if (path.Contains($"/{module.Segment}", StringComparison.Ordinal))
            {
                return module.Segment;
            }
        }

        if (path.EndsWith("/examination", StringComparison.Ordinal)
            || path.EndsWith("/examination/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }

    private static IReadOnlyList<ExaminationNavItem> BuildSidebarNav()
    {
        var items = new List<ExaminationNavItem>
        {
            new()
            {
                Key = "dashboard",
                Name = "Dashboard",
                Url = "/adminportal/examination",
                IconClass = "fa-gauge-high"
            }
        };

        foreach (var module in ExaminationModuleCatalog.SidebarModules)
        {
            items.Add(new ExaminationNavItem
            {
                Key = module.Segment,
                Name = module.Name,
                Url = ExaminationModuleCatalog.Url(module),
                IconClass = module.IconClass
            });
        }

        return items;
    }
}
