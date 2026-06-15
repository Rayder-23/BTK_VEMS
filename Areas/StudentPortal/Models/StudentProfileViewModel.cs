namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentProfileViewModel
{
    public int StudentId { get; init; }

    public string RegistrationNo { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? MobileNo { get; init; }

    public string? Email { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedOn { get; init; }

    public string? ProgramName { get; init; }

    public string? ProgramCode { get; init; }

    public string? AcademicYearName { get; init; }

    public string? ClassSectionDisplay { get; init; }

    public int? RollNo { get; init; }

    public DateTime? EnrollmentDate { get; init; }

    public string? PortalUsername { get; init; }

    public string? PortalEmail { get; init; }

    public string? PortalStatus { get; init; }

    public DateTime? LastLoginAt { get; init; }
}
