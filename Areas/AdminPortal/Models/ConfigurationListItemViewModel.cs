namespace VEMS.Areas.AdminPortal.Models;

public sealed class ConfigurationListItemViewModel
{
    public int Uid { get; init; }
    public string ConfigKey { get; init; } = string.Empty;
    public string ConfigValuesPreview { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
}
