namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeTeacherLookupResult
{
    public string EmployeeId { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Designation { get; init; }

    public string? Qualification { get; init; }

    public string? Specialization { get; init; }

    public DateTime? JoinedDate { get; init; }

    public string Status { get; init; } = string.Empty;
}
