using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class SectionListItem
{
    public int SectionId { get; init; }
    public string SectionName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class SectionFormModel
{
    public int SectionId { get; set; }

    [Required(ErrorMessage = "Section name is required.")]
    [StringLength(20)]
    [Display(Name = "Section name")]
    public string SectionName { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
