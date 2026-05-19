using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentFormModel : IValidatableObject
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(30)]
    [Display(Name = "Registration no.")]
    public string RegistrationNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Admission year is required.")]
    [Range(1990, 2100)]
    [Display(Name = "Admission year")]
    public short AdmissionYear { get; set; }

    [Required(ErrorMessage = "Admission date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Admission date")]
    public DateTime? AdmissionDate { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Middle name")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Father name is required.")]
    [StringLength(100)]
    [Display(Name = "Father name")]
    public string FatherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression("^[MFO]$", ErrorMessage = "Gender must be M, F, or O.")]
    [StringLength(1)]
    public string Gender { get; set; } = "M";

    [Required(ErrorMessage = "Address line 1 is required.")]
    [StringLength(150)]
    [Display(Name = "Address line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Address line 2")]
    public string? AddressLine2 { get; set; }

    [Required(ErrorMessage = "Country is required.")]
    [Display(Name = "Country")]
    public int CountryId { get; set; }

    [Required(ErrorMessage = "Province is required.")]
    [Display(Name = "Province")]
    public int ProvinceId { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [Display(Name = "City")]
    public int CityId { get; set; }

    [StringLength(10)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [StringLength(30)]
    [Display(Name = "Roll no.")]
    public string? RollNo { get; set; }

    [StringLength(15)]
    [Display(Name = "NIC")]
    public string? NIC_No { get; set; }

    [StringLength(15)]
    [Display(Name = "B-Form")]
    public string? BFORM_No { get; set; }

    [StringLength(50)]
    public string? Nationality { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(200)]
    [Display(Name = "Status remark")]
    public string? StatusRemark { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DateOfBirth.HasValue || DateOfBirth.Value.Year < 1900)
        {
            yield return new ValidationResult(
                "Date of birth is required.",
                [nameof(DateOfBirth)]);
            yield break;
        }

        if (DateOfBirth.Value.Date >= DateTime.Today)
        {
            yield return new ValidationResult(
                "Date of birth must be before today.",
                [nameof(DateOfBirth)]);
        }
    }
}
