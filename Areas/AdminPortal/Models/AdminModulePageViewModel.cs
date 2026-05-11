namespace VEMS.Areas.AdminPortal.Models;

public sealed class AdminModulePageViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string IconClass { get; init; } = string.Empty;

    public string AccentClass { get; init; } = string.Empty;

    public string AddButtonText { get; init; } = string.Empty;

    public IReadOnlyList<string> TableHeaders { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string>> TableRows { get; init; } = [];
}
