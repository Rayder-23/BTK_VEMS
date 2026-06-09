using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.TeacherPortal.Models;

public sealed class ClassListItem
{
    public int Uid { get; init; }

    public string ClassName { get; init; } = string.Empty;

    public string ClassCode { get; init; } = string.Empty;

    public string ProgramName { get; init; } = string.Empty;

    public byte SemesterNo { get; init; }

    public string Semester { get; init; } = string.Empty;

    public short AcademicYear { get; init; }

    public string? Section { get; init; }

    public string? Shift { get; init; }

    public string? RoomNo { get; init; }

    public short MaxStrength { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ClassLookupItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class ClassLookups
{
    public IReadOnlyList<ClassLookupItem> Programs { get; init; } = [];

    public IReadOnlyList<string> Semesters { get; init; } = [];

    public IReadOnlyList<string> Shifts { get; init; } = [];
}

public sealed class ClassFormPageViewModel
{
    public ClassFormModel Form { get; set; } = new();

    public ClassLookups Lookups { get; set; } = new();
}

public sealed class ClassFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(100)]
    [Display(Name = "Class name")]
    public string ClassName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Class code is required.")]
    [StringLength(30)]
    [Display(Name = "Class code")]
    public string ClassCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Program is required.")]
    [Display(Name = "Program")]
    [Range(1, int.MaxValue)]
    public int ProgramId { get; set; }

    [Required(ErrorMessage = "Semester number is required.")]
    [Display(Name = "Semester no.")]
    [Range(1, 12)]
    public byte SemesterNo { get; set; } = 1;

    [Required(ErrorMessage = "Semester is required.")]
    [StringLength(20)]
    public string Semester { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic year is required.")]
    [Display(Name = "Academic year")]
    [Range(2000, 2100)]
    public short AcademicYear { get; set; } = (short)DateTime.UtcNow.Year;

    [StringLength(10)]
    public string? Section { get; set; }

    [StringLength(20)]
    public string? Shift { get; set; }

    [StringLength(30)]
    [Display(Name = "Room no.")]
    public string? RoomNo { get; set; }

    [Display(Name = "Max strength")]
    [Range(1, short.MaxValue)]
    public short MaxStrength { get; set; } = 40;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? Remarks { get; set; }
}
