using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ProgramListItem
{
    public int ProgramId { get; init; }
    public string ProgramCode { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public int? DurationYears { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedOn { get; init; }
}

public sealed class ProgramFormModel
{
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Program code is required.")]
    [StringLength(20)]
    [Display(Name = "Program code")]
    public string ProgramCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program name is required.")]
    [StringLength(200)]
    [Display(Name = "Program name")]
    public string ProgramName { get; set; } = string.Empty;

    [Display(Name = "Duration (years)")]
    [Range(1, 100)]
    public int? DurationYears { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created on")]
    public DateTime? CreatedOn { get; set; }
}

public sealed class ProgramCoursesPageViewModel
{
    public int? SelectedProgramId { get; set; }
    public ProgramFormModel? Program { get; set; }
    public ProgramListItem? ProgramSummary { get; set; }
    public IReadOnlyList<StudentLookupItem> Programs { get; set; } = [];
    public IReadOnlyList<CourseListItem> Courses { get; set; } = [];
    public string? Search { get; set; }
    public bool ShowInactive { get; set; }
}
