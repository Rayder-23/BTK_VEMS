namespace VEMS.Models;

public sealed class StudentApplicationPageViewModel
{
    public StudentApplicationFormModel Form { get; set; } = new();

    public IReadOnlyList<StudentApplicationProgramOption> Programs { get; init; } = Array.Empty<StudentApplicationProgramOption>();
    public IReadOnlyList<string> Countries { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Provinces { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cities { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Genders { get; init; } = Array.Empty<string>();

    public string? SuccessMessage { get; set; }

    public string? ReferenceNo { get; set; }
}
