namespace VEMS.Areas.AdminPortal.Models.Examination;

public sealed class ExaminationDashboardTile
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
    public required string AccentClass { get; init; }
}

/// <summary>Generic table for examination module browse pages.</summary>
public sealed class ExaminationListPageViewModel
{
    public required string PageTitle { get; init; }
    public required string ModuleKey { get; init; }
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
