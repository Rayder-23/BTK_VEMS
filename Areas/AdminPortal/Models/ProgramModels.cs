using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ProgramListItem
{
    public int Uid { get; init; }
    public string ProgramCode { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public byte? DurationYears { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ProgramFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Program code is required.")]
    [StringLength(10)]
    [Display(Name = "Program code")]
    public string ProgramCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program name is required.")]
    [StringLength(100)]
    [Display(Name = "Program name")]
    public string ProgramName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Short name")]
    public string? ShortName { get; set; }

    [Display(Name = "Duration (years)")]
    [Range(1, 255)]
    public byte? DurationYears { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime? CreatedAt { get; set; }
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
