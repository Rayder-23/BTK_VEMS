using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

[Route("adminportal/examination/grading-scales")]
public sealed class GradingScalesController : ExaminationModuleControllerBase
{
    public GradingScalesController(IExaminationBrowseRepository examinationBrowse) : base(examinationBrowse) { }

    protected override Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken) =>
        ExaminationBrowse.ListGradingScalesAsync(cancellationToken);
}
