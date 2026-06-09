using System.ComponentModel.DataAnnotations;

namespace VEMS.Areas.AdminPortal.Models;

public sealed class EmployeeLoginListItemViewModel
{
    public int Uid { get; init; }
    public int EmployeeUid { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedOn { get; init; }
}

public sealed class EmployeeLoginLookups
{
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Statuses { get; init; } = [];
}

public sealed class EmployeeLoginFormViewModel
{
    public EmployeeLoginFormModel Form { get; set; } = new();

    public IReadOnlyList<StudentLookupItem> AvailableEmployees { get; set; } = [];

    public EmployeeLoginLookups Lookups { get; set; } = new();

    public bool IsEdit => Form.Uid > 0;

    public const string DefaultPassword = "vems26";
}

public sealed class EmployeeLoginFormModel
{
    public int Uid { get; set; }

    [Display(Name = "Employee")]
    [Range(1, int.MaxValue, ErrorMessage = "Select an employee.")]
    public int EmployeeUid { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters.")]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    public string? EmployeeDisplayName { get; set; }

    public string? EmployeeCode { get; set; }
}
