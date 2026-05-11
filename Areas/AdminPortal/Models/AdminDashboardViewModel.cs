namespace VEMS.Areas.AdminPortal.Models;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<AdminStatisticCard> Statistics { get; init; } = [];

    public IReadOnlyList<AdminModuleCard> Modules { get; init; } = [];
}

public sealed class AdminStatisticCard
{
    public string Title { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string IconClass { get; init; } = string.Empty;

    public string AccentClass { get; init; } = string.Empty;
}

public sealed class AdminModuleCard
{
    public string Name { get; init; } = string.Empty;

    public string Controller { get; init; } = string.Empty;

    public string IconClass { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string AccentClass { get; init; } = string.Empty;
}
