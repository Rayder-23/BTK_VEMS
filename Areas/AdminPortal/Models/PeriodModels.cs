using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class PeriodListItem
{
    public int PeriodId { get; init; }
    public string? PeriodName { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
}

public sealed class PeriodFormModel
{
    public int PeriodId { get; set; }

    [StringLength(50)]
    [Display(Name = "Period name")]
    public string? PeriodName { get; set; }

    [Display(Name = "Start time")]
    public TimeSpan? StartTime { get; set; }

    [Display(Name = "End time")]
    public TimeSpan? EndTime { get; set; }
}
