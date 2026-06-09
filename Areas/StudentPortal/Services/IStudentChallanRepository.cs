using VEMS.Areas.StudentPortal.Models;

namespace VEMS.Areas.StudentPortal.Services;

public interface IStudentChallanRepository
{
    Task<StudentChallanPageModel> GetCurrentMonthChallanAsync(int studentUid, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentChallanSummary>> ListChallanHistoryAsync(int studentUid, CancellationToken cancellationToken = default);
}
