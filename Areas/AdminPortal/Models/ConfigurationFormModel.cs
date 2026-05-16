using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class ConfigurationFormModel
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Config key is required.")]
    [StringLength(100)]
    [Display(Name = "Config key")]
    public string ConfigKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Config value is required.")]
    [Display(Name = "Config value")]
    public string ConfigValues { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Last updated")]
    public DateTime? UpdatedAt { get; set; }
}
