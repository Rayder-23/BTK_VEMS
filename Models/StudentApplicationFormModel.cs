using System.ComponentModel.DataAnnotations;

namespace VEMS.Models;

public sealed class StudentApplicationFormModel
{
    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    public string ProgramCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Institution type is required.")]
    [StringLength(10)]
    [Display(Name = "Institution type")]
    public string InstTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program name is required.")]
    [StringLength(100)]
    [Display(Name = "Program name")]
    public string ProgramName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Desired year is required.")]
    [Range(2020, 2100)]
    [Display(Name = "Desired admission year")]
    public short DesiredYear { get; set; }

    [Required(ErrorMessage = "Grade or semester is required.")]
    [Range(1, 20)]
    [Display(Name = "Desired grade / semester")]
    public byte DesiredGradeOrSemester { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Father name is required.")]
    [StringLength(100)]
    [Display(Name = "Father / guardian name")]
    public string FatherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(1)]
    public string Gender { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "B-Form no.")]
    public string? BFORM_No { get; set; }

    [StringLength(15)]
    [Display(Name = "CNIC / NIC")]
    public string? NIC_No { get; set; }

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid mobile number.")]
    [Display(Name = "Mobile number")]
    public string MobileNo { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string? EmailAddress { get; set; }

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(150)]
    [Display(Name = "Address")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Province")]
    public string? Province { get; set; }

    [StringLength(100)]
    [Display(Name = "Country")]
    public string Country { get; set; } = "Pakistan";

    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid mobile number.")]
    [Display(Name = "Father mobile")]
    public string? FatherMobile { get; set; }

    [StringLength(100)]
    [Display(Name = "Father occupation")]
    public string? FatherOccupation { get; set; }
}
