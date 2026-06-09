using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ProgramListItem
{
    public int Uid { get; init; }
    public string ProgramCode { get; init; } = string.Empty;
    public string ProgramName { get; init; } = string.Empty;
    public string InstitutionTypeName { get; init; } = string.Empty;
    public string? DegreeLevel { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class ProgramLookups
{
    public IReadOnlyList<StudentLookupItem> InstitutionTypes { get; init; } = [];
    public IReadOnlyList<string> ProgramLevels { get; init; } = [];
    public IReadOnlyList<string> ProgramTypes { get; init; } = [];
    public IReadOnlyList<string> DegreeLevels { get; init; } = [];
    public IReadOnlyList<string> ProgramStatuses { get; init; } = [];
}

public sealed class ProgramFormPageViewModel
{
    public ProgramFormModel Form { get; set; } = new();
    public ProgramLookups Lookups { get; set; } = new();
}

public sealed class ProgramFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Institution type is required.")]
    [Display(Name = "Institution type")]
    [Range(1, int.MaxValue)]
    public int InstTypeId { get; set; }

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

    [StringLength(50)]
    [Display(Name = "Program level")]
    public string? ProgramLevel { get; set; }

    [StringLength(50)]
    [Display(Name = "Program type")]
    public string? ProgramType { get; set; }

    [StringLength(50)]
    [Display(Name = "Degree level")]
    public string? DegreeLevel { get; set; }

    [Display(Name = "Duration (years)")]
    [Range(1, 10)]
    public byte? DurationYears { get; set; }

    [Display(Name = "Total semesters")]
    [Range(1, 255)]
    public byte? TotalSemesters { get; set; }

    [Display(Name = "Total grades")]
    [Range(1, 255)]
    public byte? TotalGrades { get; set; }

    [Display(Name = "Total credit hours")]
    [Range(1, 32767)]
    public short? TotalCreditHours { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
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
