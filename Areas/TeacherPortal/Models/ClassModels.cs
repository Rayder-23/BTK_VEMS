using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.TeacherPortal.Models;

public sealed class ClassListItem
{
    public int ClassId { get; init; }
    public string ClassCode { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public int? SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ClassFormModel
{
    public int ClassId { get; set; }

    [StringLength(20)]
    [Display(Name = "Class code")]
    public string? ClassCode { get; set; }

    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(100)]
    [Display(Name = "Class name")]
    public string ClassName { get; set; } = string.Empty;

    [Display(Name = "Sort order")]
    public int? SortOrder { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public sealed class ClassFormPageViewModel
{
    public ClassFormModel Form { get; set; } = new();
}
