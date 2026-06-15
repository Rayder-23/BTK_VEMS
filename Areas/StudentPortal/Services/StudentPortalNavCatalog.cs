namespace VEMS.Areas.StudentPortal.Services;

public sealed class StudentPortalNavItem
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Controller { get; init; }
    public required string Action { get; init; }
    public required string IconClass { get; init; }
}

public sealed class StudentPortalNavModule
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string IconClass { get; init; }
    public required string DefaultController { get; init; }
    public required string DefaultAction { get; init; }
    public required IReadOnlyList<StudentPortalNavItem> Links { get; init; }
}

public static class StudentPortalNavCatalog
{
    public static IReadOnlyList<StudentPortalNavModule> TopBarModules { get; } =
    [
        new()
        {
            Key = "my-courses",
            Name = "My Courses",
            IconClass = "bi-journal-bookmark",
            DefaultController = "Courses",
            DefaultAction = "AllCourses",
            Links =
            [
                new() { Key = "enrolled-courses", Name = "Enrolled Courses", Controller = "Courses", Action = "AllCourses", IconClass = "bi-collection" },
                new() { Key = "course-materials", Name = "Course Materials", Controller = "Courses", Action = "CourseMaterials", IconClass = "bi-folder2-open" },
                new() { Key = "lesson-plans", Name = "Lesson Plans", Controller = "Courses", Action = "LessonPlans", IconClass = "bi-journal-text" }
            ]
        },
        new()
        {
            Key = "fee",
            Name = "Fee",
            IconClass = "bi-wallet2",
            DefaultController = "Fees",
            DefaultAction = "Challan",
            Links =
            [
                new() { Key = "challan", Name = "Challan", Controller = "Fees", Action = "Challan", IconClass = "bi-receipt" },
                new() { Key = "fee-history", Name = "Fee History", Controller = "Fees", Action = "FeeHistory", IconClass = "bi-clock-history" }
            ]
        },
        new()
        {
            Key = "assessments",
            Name = "Assessments",
            IconClass = "bi-clipboard-data",
            DefaultController = "Results",
            DefaultAction = "Assignments",
            Links =
            [
                new() { Key = "assignments", Name = "Assignments", Controller = "Results", Action = "Assignments", IconClass = "bi-file-earmark-text" },
                new() { Key = "quizzes", Name = "Quizzes", Controller = "Results", Action = "Quizzes", IconClass = "bi-patch-question" },
                new() { Key = "exams", Name = "Exams", Controller = "Results", Action = "Exams", IconClass = "bi-journal-check" },
                new() { Key = "grades", Name = "Grades", Controller = "Results", Action = "Grades", IconClass = "bi-award" }
            ]
        },
        new()
        {
            Key = "settings",
            Name = "Settings",
            IconClass = "bi-gear",
            DefaultController = "Settings",
            DefaultAction = "ChangePassword",
            Links =
            [
                new() { Key = "change-password", Name = "Change Password", Controller = "Settings", Action = "ChangePassword", IconClass = "bi-shield-lock" },
                new() { Key = "change-theme", Name = "Change Theme", Controller = "Settings", Action = "ChangeTheme", IconClass = "bi-palette" }
            ]
        }
    ];

    public static StudentPortalNavModule? GetModule(string moduleKey) =>
        TopBarModules.FirstOrDefault(module =>
            string.Equals(module.Key, moduleKey, StringComparison.OrdinalIgnoreCase));

    public static string? ResolveModuleKey(string controller, string action)
    {
        var activeKey = ResolveActiveKey(controller, action);
        return TopBarModules.FirstOrDefault(module =>
            module.Links.Any(link => string.Equals(link.Key, activeKey, StringComparison.OrdinalIgnoreCase)))?.Key;
    }

    public static StudentPortalNavModule? GetModuleForRoute(string controller, string action)
    {
        var moduleKey = ResolveModuleKey(controller, action);
        return moduleKey is null ? null : GetModule(moduleKey);
    }

    public static string ResolveActiveKey(string controller, string action)
    {
        controller = controller.Trim();
        action = action.Trim();

        foreach (var module in TopBarModules)
        {
            foreach (var link in module.Links)
            {
                if (string.Equals(link.Controller, controller, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(link.Action, action, StringComparison.OrdinalIgnoreCase))
                {
                    return link.Key;
                }
            }
        }

        if (string.Equals(controller, "Fees", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "CurrentMonth", StringComparison.OrdinalIgnoreCase))
        {
            return "challan";
        }

        if (string.Equals(controller, "Fees", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "PreviousFee", StringComparison.OrdinalIgnoreCase))
        {
            return "fee-history";
        }

        if (string.Equals(controller, "Settings", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
        {
            return "change-password";
        }

        return string.Empty;
    }

    public static bool IsModuleRoute(string controller, string action) =>
        !string.IsNullOrEmpty(ResolveActiveKey(controller, action));
}
