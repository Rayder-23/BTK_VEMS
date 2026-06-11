namespace VEMS.Areas.AdminPortal.Services;

public sealed class TeachersModule
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
    public required string Description { get; init; }
    public required string AccentClass { get; init; }
}

public static class TeachersModuleCatalog
{
    public static IReadOnlyList<TeachersModule> GridModules { get; } =
    [
        new()
        {
            Name = "Add Teachers",
            Url = "/adminportal/teachers/create",
            IconClass = "bi-person-plus-fill",
            Description = "Register faculty from active employee records",
            AccentClass = "accent-blue"
        },
        new()
        {
            Name = "All Teachers",
            Url = "/adminportal/teachers/all",
            IconClass = "bi-person-workspace",
            Description = "Browse and manage teacher profiles",
            AccentClass = "accent-purple"
        },
        new()
        {
            Name = "Teachers-Class-Course",
            Url = "/adminportal/teachers/teacher-class-courses",
            IconClass = "bi-link-45deg",
            Description = "Assign teachers to class courses",
            AccentClass = "accent-teal"
        }
    ];
}
