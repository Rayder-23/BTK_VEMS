namespace VEMS.Areas.AdminPortal.Services;

public sealed class SettingsNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string IconClass { get; init; }
}

public sealed class SettingsNavModule
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string IconClass { get; init; }
    public required string DefaultUrl { get; init; }
    public required IReadOnlyList<SettingsNavItem> Links { get; init; }
}

public static class SettingsNavCatalog
{
    public static SettingsNavModule HomeModule { get; } = new()
    {
        Key = "home",
        Name = "Home",
        IconClass = "fa-gear",
        DefaultUrl = "/adminportal/settings",
        Links =
        [
            new() { Key = "dashboard", Name = "Dashboard", Url = "/adminportal/settings", IconClass = "fa-gauge-high" }
        ]
    };

    public static IReadOnlyList<SettingsNavModule> TopBarModules { get; } =
    [
        new()
        {
            Key = "config",
            Name = "Config",
            IconClass = "fa-gear",
            DefaultUrl = "/adminportal/settings/configurations",
            Links =
            [
                new() { Key = "configurations", Name = "Configurations", Url = "/adminportal/settings/configurations", IconClass = "fa-sliders" },
                new() { Key = "academic-year", Name = "AcademicYear", Url = "/adminportal/settings/academic-years", IconClass = "fa-calendar-days" },
                new() { Key = "programs", Name = "Programs", Url = "/adminportal/students/programs", IconClass = "fa-graduation-cap" },
                new() { Key = "classes", Name = "Classes", Url = "/adminportal/students/classes", IconClass = "fa-chalkboard" },
                new() { Key = "courses", Name = "Courses", Url = "/adminportal/settings/courses", IconClass = "fa-book" },
                new() { Key = "sections", Name = "Sections", Url = "/adminportal/settings/sections", IconClass = "fa-layer-group" },
                new() { Key = "periods", Name = "Periods", Url = "/adminportal/settings/periods", IconClass = "fa-clock" },
                new() { Key = "timetables", Name = "Timetable", Url = "/adminportal/settings/timetables", IconClass = "fa-calendar-week" }
            ]
        },
        new()
        {
            Key = "link-tables",
            Name = "Link Tables",
            IconClass = "fa-link",
            DefaultUrl = "/adminportal/settings/program-courses",
            Links =
            [
                new() { Key = "program-courses", Name = "Link_ProgramCourses", Url = "/adminportal/settings/program-courses", IconClass = "fa-link" },
                new() { Key = "teacher-courses", Name = "Link_TeacherCourses", Url = "/adminportal/settings/teacher-courses", IconClass = "fa-link" },
                new() { Key = "class-sections", Name = "Link_ClassSections", Url = "/adminportal/settings/class-sections", IconClass = "fa-link" },
                new() { Key = "class-section-courses", Name = "Link_ClassSectionCoursees", Url = "/adminportal/settings/class-section-courses", IconClass = "fa-link" },
                new() { Key = "course-sections", Name = "Uni_CourseSections", Url = "/adminportal/settings/course-sections", IconClass = "fa-link" },
                new() { Key = "student-course-registrations", Name = "Uni_StudentCourseRegistrations", Url = "/adminportal/settings/student-course-registrations", IconClass = "fa-link" }
            ]
        }
    ];

    public static IReadOnlyList<SettingsNavItem> SidebarPlaceholderNav { get; } =
    [
        new() { Key = "placeholder-1", Name = "", Url = "#", IconClass = "" }
    ];

    public static IReadOnlyList<SettingsNavItem> SidebarNav { get; } = [];

    public static bool IsSettingsController(string controller) =>
        string.Equals(controller, "Settings", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "Configurations", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "AcademicYears", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "StudentPrograms", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "Classes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "Sections", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "ClassSections", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "StudentCourses", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "ProgramCourses", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "TeacherCourseLinks", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "ClassSectionCourseLinks", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "CourseSections", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "StudentCourseRegistrations", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "Periods", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "Timetables", StringComparison.OrdinalIgnoreCase);

    public static SettingsNavModule? GetModule(string moduleKey) =>
        TopBarModules.FirstOrDefault(module =>
            string.Equals(module.Key, moduleKey, StringComparison.OrdinalIgnoreCase));

    public static string? ResolveModuleKey(string path, string action)
    {
        var activeKey = ResolveActiveKey(path, action);
        var moduleKey = TopBarModules.FirstOrDefault(module =>
            module.Links.Any(link => string.Equals(link.Key, activeKey, StringComparison.OrdinalIgnoreCase)))?.Key;

        if (moduleKey is not null)
        {
            return moduleKey;
        }

        return HomeModule.Links.Any(link =>
            string.Equals(link.Key, activeKey, StringComparison.OrdinalIgnoreCase))
            ? HomeModule.Key
            : null;
    }

    public static SettingsNavModule? GetModuleForRoute(string path, string action)
    {
        var moduleKey = ResolveModuleKey(path, action);
        if (moduleKey is null)
        {
            return null;
        }

        return string.Equals(moduleKey, HomeModule.Key, StringComparison.OrdinalIgnoreCase)
            ? HomeModule
            : GetModule(moduleKey);
    }

    public static string ResolveActiveKey(string path, string action)
    {
        path = path.TrimEnd('/').ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (path.Contains("/settings/configurations", StringComparison.Ordinal))
        {
            return "configurations";
        }

        if (path.Contains("/academic-years", StringComparison.Ordinal))
        {
            return "academic-year";
        }

        if (path.Contains("/settings/courses", StringComparison.Ordinal))
        {
            return "courses";
        }

        if (path.Contains("/settings/sections", StringComparison.Ordinal)
            && !path.Contains("/class-sections", StringComparison.Ordinal)
            && !path.Contains("/course-sections", StringComparison.Ordinal))
        {
            return "sections";
        }

        if (path.Contains("/students/classes", StringComparison.Ordinal)
            || path.EndsWith("/classes", StringComparison.Ordinal))
        {
            return "classes";
        }

        if (path.Contains("/students/programs", StringComparison.Ordinal)
            || (path.Contains("/programs", StringComparison.Ordinal)
                && !path.Contains("/program-courses", StringComparison.Ordinal)))
        {
            return "programs";
        }

        if (path.Contains("/teacher-courses", StringComparison.Ordinal))
        {
            return "teacher-courses";
        }

        if (path.Contains("/program-courses", StringComparison.Ordinal))
        {
            return "program-courses";
        }

        if (path.Contains("/class-section-courses", StringComparison.Ordinal))
        {
            return "class-section-courses";
        }

        if (path.Contains("/student-course-registrations", StringComparison.Ordinal))
        {
            return "student-course-registrations";
        }

        if (path.Contains("/course-sections", StringComparison.Ordinal))
        {
            return "course-sections";
        }

        if (path.Contains("/settings/timetables", StringComparison.Ordinal))
        {
            return "timetables";
        }

        if (path.Contains("/periods", StringComparison.Ordinal))
        {
            return "periods";
        }

        if (path.Contains("/class-sections", StringComparison.Ordinal))
        {
            return "class-sections";
        }

        if (path.Contains("/settings/create", StringComparison.Ordinal))
        {
            return "configurations";
        }

        if (path.EndsWith("/settings", StringComparison.Ordinal) || path.EndsWith("/settings/index", StringComparison.Ordinal))
        {
            return "dashboard";
        }

        return "dashboard";
    }
}
