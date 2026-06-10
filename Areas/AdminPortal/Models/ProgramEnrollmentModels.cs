using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ProgramEnrollmentListItem
{
    public int Uid { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string RegistrationNo { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public short AcademicYear { get; init; }
    public byte GradeOrSemester { get; init; }
    public string RollNo { get; init; } = string.Empty;
    public string EnrollmentStatus { get; init; } = string.Empty;
    public string FeeStatus { get; init; } = string.Empty;
}

public sealed class ProgramEnrollmentLookups
{
    public IReadOnlyList<StudentLookupItem> Students { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Sections { get; init; } = [];
    public IReadOnlyList<string> EnrollmentStatuses { get; init; } = [];
    public IReadOnlyList<string> FeeStatuses { get; init; } = [];
}

public sealed class ProgramEnrollmentFormPageViewModel
{
    public ProgramEnrollmentFormModel Form { get; set; } = new();
    public ProgramEnrollmentLookups Lookups { get; set; } = new();
}

public sealed class ProgramEnrollmentFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Student is required.")]
    [Display(Name = "Student")]
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(1990, 2100)]
    public short AcademicYear { get; set; }

    [Required(ErrorMessage = "Grade or semester is required.")]
    [Display(Name = "Grade / semester")]
    [Range(1, 255)]
    public byte GradeOrSemester { get; set; } = 1;

    [Required(ErrorMessage = "Section is required.")]
    [Display(Name = "Section")]
    [Range(1, int.MaxValue)]
    public int SectionId { get; set; } = 1;

    [Required(ErrorMessage = "Roll number is required.")]
    [StringLength(30)]
    [Display(Name = "Roll no.")]
    public string RollNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enrollment date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Enrollment date")]
    public DateTime? EnrollmentDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Completion date")]
    public DateTime? CompletionDate { get; set; }

    [Required(ErrorMessage = "Enrollment status is required.")]
    [Display(Name = "Enrollment status")]
    [StringLength(20)]
    public string EnrollmentStatus { get; set; } = "Active";

    [Required(ErrorMessage = "Fee status is required.")]
    [Display(Name = "Fee status")]
    [StringLength(20)]
    public string FeeStatus { get; set; } = "Pending";

    [StringLength(300)]
    public string? Remarks { get; set; }
}
