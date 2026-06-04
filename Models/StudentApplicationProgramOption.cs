namespace VEMS.Models;

public sealed class StudentApplicationProgramOption
{
    public string InstTypeCode { get; init; } = string.Empty;
    public string ProgramCode { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public byte? TotalGrades { get; init; }
    public byte? TotalSemesters { get; init; }

    /// <summary>Dropdown text from ref_Programs.ProgramName.</summary>
    public string DisplayLabel => ProgramName;
}
