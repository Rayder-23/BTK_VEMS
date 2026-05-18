namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentLookups
{
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Countries { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Provinces { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Cities { get; init; } = [];
}
