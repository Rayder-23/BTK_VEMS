namespace VEMS.Areas.AdminPortal.Models;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<AdminModuleCard> Modules { get; init; } = [];
}

public sealed class AdminModuleCard
{
    public string Name { get; init; } = string.Empty;

    public string Controller { get; init; } = string.Empty;

    public string IconClass { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string AccentClass { get; init; } = string.Empty;

    /// <summary>Cover image URL for the dashboard module tile.</summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>When false, the module appears on the dashboard but is not linked in navigation yet.</summary>
    public bool IsAvailable { get; init; } = true;

    /// <summary>Custom URL when the module does not follow /adminportal/{controller}.</summary>
    public string? UrlOverride { get; init; }
}
