namespace VEMS.Models;

public sealed class StudentApplicationProgramOption
{
    public string InstTypeCode { get; init; } = string.Empty;
    public string ProgramCode { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public byte? DurationYears { get; init; }

    /// <summary>Upper bound for desired grade/semester (derived from DurationYears when set).</summary>
    public byte MaxDesiredLevel =>
        DurationYears is > 0 and var years
            ? (byte)Math.Min(years * 2, 20)
            : (byte)12;

    /// <summary>Dropdown text from ref_Programs.ProgramName.</summary>
    public string DisplayLabel => ProgramName;
}
