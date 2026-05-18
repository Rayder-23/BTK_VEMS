namespace VEMS.Areas.AdminPortal.Models;

public sealed class StudentLoginFormViewModel
{
    public StudentLoginFormModel Form { get; set; } = new();

    public IReadOnlyList<StudentLookupItem> AvailableStudents { get; set; } = [];

    public bool IsEdit => Form.Uid > 0;

    public const string DefaultPassword = "vems26";
}
