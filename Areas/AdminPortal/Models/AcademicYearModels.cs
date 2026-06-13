using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class AcademicYearListItem
{
    public int AcademicYearId { get; init; }

    public string YearName { get; init; } = string.Empty;

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public bool IsCurrent { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedOn { get; init; }
}

public sealed class AcademicYearFormModel
{
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Year name is required.")]
    [StringLength(20)]
    [Display(Name = "Year name")]
    public string YearName { get; set; } = string.Empty;

    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Current year")]
    public bool IsCurrent { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created on")]
    [DataType(DataType.DateTime)]
    public DateTime? CreatedOn { get; set; }
}
