using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class CourseSectionListItem
{
    public int CourseSectionId { get; init; }
    public string YearName { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string? SectionName { get; init; }
    public int? Capacity { get; init; }
}

public sealed class CourseSectionLookups
{
    public IReadOnlyList<StudentLookupItem> AcademicYears { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Courses { get; init; } = [];
}

public sealed class CourseSectionFormPageViewModel
{
    public CourseSectionFormModel Form { get; set; } = new();
    public CourseSectionLookups Lookups { get; set; } = new();
}

public sealed class CourseSectionFormModel
{
    public int CourseSectionId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [Display(Name = "Course")]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [StringLength(20)]
    [Display(Name = "Section name")]
    public string? SectionName { get; set; }

    [Display(Name = "Capacity")]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1 when provided.")]
    public int? Capacity { get; set; }
}
