namespace VEMS.Areas.AdminPortal.Models;

public sealed class HrFormPlaceholderViewModel
{
    public string ModuleName { get; init; } = string.Empty;

    public string ModuleKey { get; init; } = string.Empty;

    public string Segment { get; init; } = string.Empty;

    public string IconClass { get; init; } = string.Empty;

    public string AccentClass { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int? Id { get; init; }

    public bool IsEdit => Id.HasValue;
}
