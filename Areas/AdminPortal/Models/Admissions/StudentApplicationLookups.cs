using VEMS.Models;

namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class StudentApplicationLookups
{
    public IReadOnlyList<StudentApplicationProgramOption> Programs { get; init; } = Array.Empty<StudentApplicationProgramOption>();
    public IReadOnlyList<string> ApplicationStatuses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SourceChannels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TestStatuses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PaymentStatuses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Genders { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Countries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Provinces { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cities { get; init; } = Array.Empty<string>();
}
