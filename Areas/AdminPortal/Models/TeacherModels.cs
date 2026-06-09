using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class TeacherListItemViewModel
{
    public int Uid { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? Designation { get; init; }

    public string? ProgramName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public bool IsActive { get; init; }
}

public sealed class TeacherLookups
{
    public IReadOnlyList<StudentLookupItem> Programs { get; init; } = [];
}

public sealed class TeacherFormPageViewModel
{
    public TeacherFormModel Form { get; set; } = new();

    public TeacherLookups Lookups { get; set; } = new();
}

public sealed class TeacherFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Employee code is required.")]
    [StringLength(20)]
    [Display(Name = "Employee code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(150)]
    public string? Qualification { get; set; }

    [StringLength(200)]
    public string? Specialization { get; set; }

    [Display(Name = "Program")]
    public int? ProgramId { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Joining date")]
    public DateTime? JoiningDate { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? Remarks { get; set; }
}
