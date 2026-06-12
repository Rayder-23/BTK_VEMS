using VEMS.Areas.AdminPortal.Services.Admissions;

namespace VEMS.Areas.AdminPortal.Models.Admissions;

public sealed class StudentApplicationFormViewModel
{
    public StudentApplicationFormModel Form { get; set; } = new();
    public StudentApplicationLookups Lookups { get; set; } = new();

    public bool IsReadOnly =>
        string.Equals(Form.ApplicationStatus, StudentApplicationAdminRepository.ConvertedApplicationStatus, StringComparison.OrdinalIgnoreCase);

    public bool ShowConvertAsStudentButton =>
        !IsReadOnly
        && string.Equals(Form.ApplicationStatus, StudentApplicationAdminRepository.ApprovedApplicationStatus, StringComparison.OrdinalIgnoreCase);
}
