namespace VEMS.Areas.AdminPortal.Services;

/// <summary>
/// Allowed values for <c>dbo.Teachers.Designation</c> per <c>CK_Teachers_Designation</c>.
/// </summary>
public static class TeacherDesignationCatalog
{
    public static IReadOnlyList<string> Allowed { get; } =
    [
        "Lecturer",
        "Senior Lecturer",
        "Assistant Professor",
        "Associate Professor",
        "Professor",
        "Visiting Lecturer",
        "Lab Instructor",
        "HOD",
        "Other"
    ];
}
