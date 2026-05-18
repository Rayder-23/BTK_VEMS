namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentFormViewModel
{
    public StudentFormModel Form { get; set; } = new();
    public StudentLookups Lookups { get; set; } = new();
}
