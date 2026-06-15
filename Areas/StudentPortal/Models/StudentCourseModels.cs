namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentEnrollmentContext
{
    public int EnrollmentId { get; init; }

    public int ProgramId { get; init; }

    public string ProgramName { get; init; } = string.Empty;

    public string ProgramCode { get; init; } = string.Empty;

    public string AcademicYearName { get; init; } = string.Empty;

    public string? ClassSectionDisplay { get; init; }

    public int? RollNo { get; init; }

    public DateTime EnrollmentDate { get; init; }
}

public sealed class StudentAssignedCourseItem
{
    public int CourseId { get; init; }

    public int EnrollmentId { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string ProgramName { get; init; } = string.Empty;

    public string ProgramCode { get; init; } = string.Empty;

    public int? CreditHours { get; init; }

    public string AcademicYearName { get; init; } = string.Empty;

    public string? ClassSectionDisplay { get; init; }

    public bool IsActive { get; init; }
}

public sealed class StudentAllCoursesPageModel
{
    public string? ProgramName { get; init; }

    public string? ProgramCode { get; init; }

    public int? RollNo { get; init; }

    public string? AcademicYearName { get; init; }

    public IReadOnlyList<StudentEnrollmentContext> Enrollments { get; init; } = [];

    public IReadOnlyList<StudentAssignedCourseItem> Courses { get; init; } = [];
}
