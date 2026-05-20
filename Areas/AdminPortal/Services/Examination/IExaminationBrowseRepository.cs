using VEMS.Areas.AdminPortal.Models.Examination;

namespace VEMS.Areas.AdminPortal.Services.Examination;

public interface IExaminationBrowseRepository
{
    Task<ExaminationListPageViewModel> ListExamTypesAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListGradingScalesAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListGradingScaleDetailsAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListMarkingSchemesAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListExamsAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListExamSchedulesAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListStudentMarksAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListAssignmentSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListStudentGradesAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListStudentResultsAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListResultDetailsAsync(CancellationToken cancellationToken = default);
    Task<ExaminationListPageViewModel> ListGradeRemarksAsync(CancellationToken cancellationToken = default);
}
