using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

[Route("adminportal/examination/grading-scale-details")]
public sealed class GradingScaleDetailsController : ExaminationModuleControllerBase
{
    public GradingScaleDetailsController(IExaminationBrowseRepository examinationBrowse) : base(examinationBrowse) { }

    protected override Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken) =>
        ExaminationBrowse.ListGradingScaleDetailsAsync(cancellationToken);
}
