using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class StudentApplicationFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Application number is required.")]
    [StringLength(20)]
    [Display(Name = "Application no.")]
    public string ApplicationNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Application date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Application date")]
    public DateTime? ApplicationDate { get; set; }

    [Required(ErrorMessage = "Source channel is required.")]
    [StringLength(20)]
    [Display(Name = "Source channel")]
    public string SourceChannel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Institution type is required.")]
    [StringLength(10)]
    [Display(Name = "Institution type")]
    public string InstTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program code is required.")]
    [StringLength(10)]
    [Display(Name = "Program code")]
    public string ProgramCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program name is required.")]
    [StringLength(100)]
    [Display(Name = "Program name")]
    public string ProgramName { get; set; } = string.Empty;

    [Required]
    [Range(2020, 2100)]
    [Display(Name = "Desired year")]
    public short DesiredYear { get; set; }

    [Required]
    [Range(1, 20)]
    [Display(Name = "Desired grade / semester")]
    public byte DesiredGradeOrSemester { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Father name")]
    public string FatherName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required]
    [StringLength(1)]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "B-Form no.")]
    public string? BFORM_No { get; set; }

    [StringLength(15)]
    [Display(Name = "NIC")]
    public string? NIC_No { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Mobile")]
    public string MobileNo { get; set; } = string.Empty;

    [StringLength(150)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? EmailAddress { get; set; }

    [Required]
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
    [Display(Name = "Father mobile")]
    public string? FatherMobile { get; set; }

    [StringLength(100)]
    [Display(Name = "Father occupation")]
    public string? FatherOccupation { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Application status")]
    public string ApplicationStatus { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Test status")]
    public string? TestStatus { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Test date")]
    public DateTime? TestDate { get; set; }

    [Range(0, 999.99)]
    [Display(Name = "Test score")]
    public decimal? TestScore { get; set; }

    [StringLength(500)]
    [Display(Name = "Teacher comments")]
    public string? TeacherComments { get; set; }

    [StringLength(20)]
    [Display(Name = "Payment status")]
    public string? PaymentStatus { get; set; }

    [Range(0, 99999999.99)]
    [Display(Name = "Payment amount")]
    public decimal? PaymentAmount { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Payment date")]
    public DateTime? PaymentDate { get; set; }

    [Display(Name = "Converted student ID")]
    public int? ConvertedStudentID { get; set; }

    [Display(Name = "Converted at")]
    public DateTime? ConvertedAt { get; set; }
}
