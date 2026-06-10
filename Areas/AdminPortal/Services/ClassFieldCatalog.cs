namespace VEMS.Areas.AdminPortal.Services;

public static class ClassFieldCatalog
{
    public static IReadOnlyList<string> AllowedSemesters { get; } = ["Fall", "Spring", "Summer"];

    public static IReadOnlyList<string> AllowedShifts { get; } = ["Morning", "Evening", "Weekend"];

    public static string? ResolveSemester(string? value) =>
        ResolveAllowed(value, AllowedSemesters);

    public static string? ResolveShift(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ResolveAllowed(value, AllowedShifts);
    }

    private static string? ResolveAllowed(string? value, IReadOnlyList<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
