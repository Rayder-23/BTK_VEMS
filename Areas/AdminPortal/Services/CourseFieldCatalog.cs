namespace VEMS.Areas.AdminPortal.Services;

/// <summary>
/// Allowed values for <c>dbo.Courses</c> per database CHECK constraints.
/// </summary>
public static class CourseFieldCatalog
{
    public static IReadOnlyList<string> AllowedCourseTypes { get; } =
    [
        "Theory",
        "Lab",
        "Theory+Lab",
        "Project",
        "Seminar",
        "Internship",
        "Thesis"
    ];

    public static IReadOnlyList<string> ResolveCourseTypes(IEnumerable<string> configuredValues)
    {
        var resolved = configuredValues
            .Select(ResolveCourseType)
            .Where(v => v is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return resolved.Count > 0 ? resolved : AllowedCourseTypes.ToList();
    }

    public static string? ResolveCourseType(string? value) =>
        ResolveAllowed(value, AllowedCourseTypes);

    private static string? ResolveAllowed(string? value, IReadOnlyList<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
