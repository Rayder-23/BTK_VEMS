using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

[Route("adminportal/examination/exams")]
public sealed class ExamsController : ExaminationModuleControllerBase
{
    public ExamsController(IExaminationBrowseRepository examinationBrowse) : base(examinationBrowse) { }

    protected override Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken) =>
        ExaminationBrowse.ListExamsAsync(cancellationToken);
}
