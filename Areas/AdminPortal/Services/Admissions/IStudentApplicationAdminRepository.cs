using VEMS.Areas.AdminPortal.Models.Admissions;

namespace VEMS.Areas.AdminPortal.Services.Admissions;

public interface IStudentApplicationAdminRepository
{
    Task<IReadOnlyList<StudentApplicationListItem>> ListAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default);

    Task<StudentApplicationFormModel?> GetAsync(int uid, CancellationToken cancellationToken = default);

    Task<StudentApplicationLookups> GetLookupsAsync(CancellationToken cancellationToken = default);

    Task<bool> ApplicationNoExistsAsync(string applicationNo, int? excludeUid, CancellationToken cancellationToken = default);

    Task<string> GenerateApplicationNoAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(StudentApplicationFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(StudentApplicationFormModel model, int? updatedBy, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int uid, CancellationToken cancellationToken = default);

    Task<AdmissionsDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdmissionsPaymentListItem>> ListPaymentsAsync(
        string? search,
        string? paymentStatus,
        CancellationToken cancellationToken = default);

    Task<AdmissionsSummaryViewModel> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<AdmissionFeePickResult> GetAdmissionFeeAmountAsync(
        string programCode,
        short academicYear,
        CancellationToken cancellationToken = default);

    Task<ApplicationChallanGenerateFormModel?> GetApplicationChallanPrefillAsync(
        int applicationUid,
        CancellationToken cancellationToken = default);

    Task<int> ConvertToStudentAsync(int applicationUid, int createdBy, CancellationToken cancellationToken = default);
}
