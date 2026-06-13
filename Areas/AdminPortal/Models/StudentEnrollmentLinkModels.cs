using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentEnrollmentLinkListItem
{
    public int StudentEnrollmentId { get; init; }
    public string YearName { get; init; } = string.Empty;
    public string RegistrationNo { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string? ClassSectionDisplay { get; init; }
    public int? RollNo { get; init; }
    public DateTime EnrollmentDate { get; init; }
}

public sealed class StudentEnrollmentLinkLookups
{
    public IReadOnlyList<StudentLookupItem> AcademicYears { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Students { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> ClassSections { get; init; } = [];
}

public sealed class StudentEnrollmentLinkFormPageViewModel
{
    public StudentEnrollmentLinkFormModel Form { get; set; } = new();
    public StudentEnrollmentLinkLookups Lookups { get; set; } = new();
}

public sealed class StudentEnrollmentLinkFormModel
{
    public int StudentEnrollmentId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Student is required.")]
    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Display(Name = "Class section")]
    public int? ClassSectionId { get; set; }

    [Display(Name = "Roll no.")]
    public int? RollNo { get; set; }

    [Required(ErrorMessage = "Enrollment date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Enrollment date")]
    public DateTime EnrollmentDate { get; set; } = DateTime.Today;
}
