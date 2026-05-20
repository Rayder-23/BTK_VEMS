using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

[Route("adminportal/examination/exam-types")]
public sealed class ExamTypesController : ExaminationModuleControllerBase
{
    public ExamTypesController(IExaminationBrowseRepository examinationBrowse) : base(examinationBrowse) { }

    protected override Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken) =>
        ExaminationBrowse.ListExamTypesAsync(cancellationToken);
}
