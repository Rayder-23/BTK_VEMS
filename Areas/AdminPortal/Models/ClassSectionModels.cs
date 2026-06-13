using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ClassSectionListItem
{
    public int ClassSectionId { get; init; }
    public string YearName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string SectionName { get; init; } = string.Empty;
}

public sealed class ClassSectionLookups
{
    public IReadOnlyList<StudentLookupItem> AcademicYears { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Classes { get; init; } = [];
    public IReadOnlyList<StudentLookupItem> Sections { get; init; } = [];
}

public sealed class ClassSectionFormPageViewModel
{
    public ClassSectionFormModel Form { get; set; } = new();
    public ClassSectionLookups Lookups { get; set; } = new();
}

public sealed class ClassSectionFormModel
{
    public int ClassSectionId { get; set; }

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(1, int.MaxValue)]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Class is required.")]
    [Display(Name = "Class")]
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Required(ErrorMessage = "Section is required.")]
    [Display(Name = "Section")]
    [Range(1, int.MaxValue)]
    public int SectionId { get; set; }
}
