namespace VEMS.Areas.AdminPortal.Services;

public static class TeachersNavCatalog
{
    public static bool IsTeachersController(string? controller) =>
        string.Equals(controller, "Teachers", StringComparison.OrdinalIgnoreCase)
        || string.Equals(controller, "TeacherClassCourses", StringComparison.OrdinalIgnoreCase);
}
