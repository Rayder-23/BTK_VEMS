using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentCourseEnrollmentListItem
{
    public int Uid { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string RegistrationNo { get; init; } = string.Empty;
    public string ClassCode { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public byte GradeOrSemester { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class StudentCourseEnrollmentLookups
{
    public IReadOnlyList<StudentLookupItem> Students { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ClassCourses { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ProgramEnrollments { get; init; } = [];
    public IReadOnlyList<string> Statuses { get; init; } = [];
}

public sealed class StudentCourseEnrollmentFormPageViewModel
{
    public StudentCourseEnrollmentFormModel Form { get; set; } = new();
    public StudentCourseEnrollmentLookups Lookups { get; set; } = new();
}

public sealed class StudentCourseEnrollmentFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Student is required.")]
    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Program enrollment is required.")]
    [Display(Name = "Program enrollment")]
    [Range(1, int.MaxValue)]
    public int EnrollmentId { get; set; }

    [Required(ErrorMessage = "Class course is required.")]
    [Display(Name = "Class / course")]
    [Range(1, int.MaxValue)]
    public int ClassCourseId { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public string? StudentDisplay { get; set; }
    public string? ClassCourseDisplay { get; set; }
}
