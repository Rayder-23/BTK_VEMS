namespace VEMS.Areas.TeacherPortal.Services;

public static class TeacherNavCatalog
{
    public sealed record NavLink(
        string Key,
        string Label,
        string Controller,
        string Action,
        string IconClass);

    public sealed record NavModule(
        string Key,
        string Label,
        string Controller,
        string DefaultAction,
        string IconClass,
        IReadOnlyList<NavLink> Links);

    public static IReadOnlyList<NavLink> PermanentSidebarLinks { get; } =
    [
        new("dashboard", "Dashboard", "Dashboard", "Index", "bi-speedometer2"),
        new("profile", "Profile", "Profile", "Index", "bi-person-circle")
    ];

    private static readonly HashSet<string> TopBarExcludedModuleKeys =
        new(StringComparer.OrdinalIgnoreCase) { "dashboard", "profile", "notifications" };

    public static IReadOnlyList<NavModule> TopBarModules =>
        Modules.Where(module => !TopBarExcludedModuleKeys.Contains(module.Key)).ToList();

    public static IReadOnlyList<NavModule> Modules { get; } =
    [
        new(
            Key: "dashboard",
            Label: "Dashboard",
            Controller: "Dashboard",
            DefaultAction: "Index",
            IconClass: "bi-speedometer2",
            Links: []),
        new(
            Key: "profile",
            Label: "My Profile",
            Controller: "Profile",
            DefaultAction: "Index",
            IconClass: "bi-person-circle",
            Links:
            [
                new("change-password", "Change Password", "Settings", "ChangePassword", "bi-shield-lock")
            ]),
        new(
            Key: "academic",
            Label: "Academic Management",
            Controller: "AcademicManagement",
            DefaultAction: "Classes",
            IconClass: "bi-journal-bookmark",
            Links:
            [
                new("classes", "Classes", "AcademicManagement", "Classes", "bi-easel"),
                new("courses", "Courses", "AcademicManagement", "Courses", "bi-book"),
                new("timetable", "Timetable", "AcademicManagement", "Timetable", "bi-calendar3"),
                new("lesson-plans", "Lesson Plans", "AcademicManagement", "LessonPlans", "bi-journal-text")
            ]),
        new(
            Key: "students",
            Label: "Student Management",
            Controller: "StudentManagement",
            DefaultAction: "Students",
            IconClass: "bi-people",
            Links:
            [
                new("students", "Students", "StudentManagement", "Students", "bi-people"),
                new("attendance", "Attendance", "StudentManagement", "Attendance", "bi-calendar-check"),
                new("performance", "Performance", "StudentManagement", "Performance", "bi-graph-up")
            ]),
        new(
            Key: "assessment",
            Label: "Assessment",
            Controller: "Assessment",
            DefaultAction: "Assignments",
            IconClass: "bi-clipboard-check",
            Links:
            [
                new("assignments", "Assignments", "Assessment", "Assignments", "bi-file-earmark-text"),
                new("exams", "Exams", "Assessment", "Exams", "bi-patch-question"),
                new("grade-book", "Grade Book", "Assessment", "GradeBook", "bi-journal-bookmark-fill")
            ]),
        new(
            Key: "resources",
            Label: "Learning Resources",
            Controller: "LearningResources",
            DefaultAction: "Notes",
            IconClass: "bi-folder2-open",
            Links:
            [
                new("notes", "Notes", "LearningResources", "Notes", "bi-sticky"),
                new("videos", "Videos", "LearningResources", "Videos", "bi-play-btn"),
                new("materials", "Materials", "LearningResources", "Materials", "bi-box-seam")
            ]),
        new(
            Key: "communication",
            Label: "Communication",
            Controller: "Communication",
            DefaultAction: "Messages",
            IconClass: "bi-chat-dots",
            Links:
            [
                new("messages", "Messages", "Communication", "Messages", "bi-envelope"),
                new("announcements", "Announcements", "Communication", "Announcements", "bi-megaphone"),
                new("meetings", "Meetings", "Communication", "Meetings", "bi-camera-video")
            ]),
        new(
            Key: "reports",
            Label: "Reports",
            Controller: "Reports",
            DefaultAction: "AttendanceReports",
            IconClass: "bi-bar-chart",
            Links:
            [
                new("attendance-reports", "Attendance Reports", "Reports", "AttendanceReports", "bi-calendar2-check"),
                new("marks-reports", "Marks Reports", "Reports", "MarksReports", "bi-award"),
                new("analytics", "Analytics", "Reports", "Analytics", "bi-pie-chart")
            ]),
        new(
            Key: "calendar",
            Label: "Calendar",
            Controller: "Calendar",
            DefaultAction: "AcademicCalendar",
            IconClass: "bi-calendar-event",
            Links:
            [
                new("academic-calendar", "Academic Calendar", "Calendar", "AcademicCalendar", "bi-calendar-range"),
                new("events", "Events", "Calendar", "Events", "bi-stars")
            ]),
        new(
            Key: "notifications",
            Label: "Notifications",
            Controller: "Notifications",
            DefaultAction: "Inbox",
            IconClass: "bi-bell",
            Links:
            [
                new("inbox", "Inbox", "Notifications", "Inbox", "bi-inbox"),
                new("alerts", "Alerts", "Notifications", "Alerts", "bi-exclamation-triangle")
            ]),
        new(
            Key: "settings",
            Label: "Settings",
            Controller: "Settings",
            DefaultAction: "GeneralSettings",
            IconClass: "bi-gear",
            Links:
            [
                new("general-settings", "General Settings", "Settings", "GeneralSettings", "bi-sliders"),
                new("user-settings", "User Settings", "Settings", "UserSettings", "bi-person-gear")
            ])
    ];

    private static readonly (string Controller, string Action, string ModuleKey)[] RouteModuleOverrides =
    [
        ("Settings", "ChangePassword", "profile"),
        ("Settings", "ChangeTheme", "settings"),
        ("Settings", "GeneralSettings", "settings"),
        ("Settings", "UserSettings", "settings")
    ];

    public static NavModule? GetModule(string moduleKey) =>
        Modules.FirstOrDefault(module => string.Equals(module.Key, moduleKey, StringComparison.OrdinalIgnoreCase));

    public static string ResolveModuleKey(string controller, string action)
    {
        var overrideKey = RouteModuleOverrides.FirstOrDefault(route =>
            string.Equals(route.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(route.Action, action, StringComparison.OrdinalIgnoreCase)).ModuleKey;

        if (overrideKey is not null)
        {
            return overrideKey;
        }

        var byLink = Modules.FirstOrDefault(module =>
            module.Links.Any(link =>
                string.Equals(link.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(link.Action, action, StringComparison.OrdinalIgnoreCase)));

        if (byLink is not null)
        {
            return byLink.Key;
        }

        var byController = Modules.FirstOrDefault(module =>
            string.Equals(module.Controller, controller, StringComparison.OrdinalIgnoreCase));

        return byController?.Key ?? "dashboard";
    }

    public static NavModule GetModuleForRoute(string controller, string action)
    {
        var moduleKey = ResolveModuleKey(controller, action);
        return GetModule(moduleKey) ?? Modules[0];
    }

    public static bool IsLinkActive(NavLink link, string controller, string action) =>
        string.Equals(link.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(link.Action, action, StringComparison.OrdinalIgnoreCase);

    public static bool IsPermanentSidebarLinkActive(NavLink link, string controller, string action)
    {
        if (string.Equals(link.Key, "dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(controller, "Dashboard", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(link.Key, "profile", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(controller, "Profile", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static NavLink GetModuleEntryLink(NavModule module) =>
        module.Links.Count > 0
            ? module.Links[0]
            : new NavLink(module.Key, module.Label, module.Controller, module.DefaultAction, module.IconClass);
}
