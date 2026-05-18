namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeListItemViewModel
{
    public int Uid { get; init; }
    public string EmployeeId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public string Status { get; init; } = string.Empty;
}
