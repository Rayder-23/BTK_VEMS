namespace VEMS.Services;

public static class StudentApplicationFieldDefaults
{
    /// <summary>Used when Programs no longer supplies institution type.</summary>
    public const string InstTypeCodeNotApplicable = "NA";

    public static string ResolveInstTypeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? InstTypeCodeNotApplicable : value.Trim();
}
