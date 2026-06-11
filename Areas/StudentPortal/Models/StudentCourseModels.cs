namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentEnrollmentContext
{
    public string ProgramName { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public byte GradeOrSemester { get; init; }
    public string RollNo { get; init; } = string.Empty;
}

public sealed class StudentAssignedCourseItem
{
    public int Uid { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public byte CreditHours { get; init; }
    public byte? SemesterNo { get; init; }
    public bool IsMandatory { get; init; }
    public short AcademicYear { get; init; }
    public byte GradeOrSemester { get; init; }
}

public sealed class StudentAllCoursesPageModel
{
    public string? ProgramName { get; init; }
    public string? ProgramCode { get; init; }
    public string? RollNo { get; init; }
    public short? AdmissionYear { get; init; }
    public IReadOnlyList<StudentEnrollmentContext> Enrollments { get; init; } = [];
    public IReadOnlyList<StudentAssignedCourseItem> Courses { get; init; } = [];
}
