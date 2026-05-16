namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeListItemViewModel
{
    public int Uid { get; init; }
    public string EmployeeID { get; init; } = string.Empty;
    public string? EmployeeName { get; init; }
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public string? EmployeeStatus { get; init; }
}
