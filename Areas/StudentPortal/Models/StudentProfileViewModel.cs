namespace VEMS.Areas.StudentPortal.Models;

public sealed class StudentProfileViewModel
{
    public int Uid { get; init; }

    public string RegistrationNo { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string FatherName { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string Gender { get; init; } = string.Empty;

    public string? Nationality { get; init; }

    public string? NicNo { get; init; }

    public string? BformNo { get; init; }

    public string? RollNo { get; init; }

    public string? ProgramName { get; init; }

    public short AdmissionYear { get; init; }

    public DateTime AdmissionDate { get; init; }

    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    public string? CityName { get; init; }

    public string? ProvinceName { get; init; }

    public string? CountryName { get; init; }

    public string? PostalCode { get; init; }

    public bool IsActive { get; init; }

    public string? StatusRemark { get; init; }

    public string? PortalUsername { get; init; }

    public string? PortalEmail { get; init; }

    public string? PortalStatus { get; init; }

    public DateTime? LastLoginAt { get; init; }

    public string GenderDisplay => Gender.Trim().ToUpperInvariant() switch
    {
        "M" => "Male",
        "F" => "Female",
        _ => Gender
    };
}
