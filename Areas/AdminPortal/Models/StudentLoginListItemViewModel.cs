namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentLoginListItemViewModel
{
    public int Uid { get; init; }

    public int StudentId { get; init; }

    public string Username { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public string RegistrationNo { get; init; } = string.Empty;

    public DateTime? LastLoginAt { get; init; }

    public bool MustChangePassword { get; init; }
}
