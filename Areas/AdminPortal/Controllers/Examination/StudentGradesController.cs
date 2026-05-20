using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

[Route("adminportal/examination/student-grades")]
public sealed class StudentGradesController : ExaminationModuleControllerBase
{
    public StudentGradesController(IExaminationBrowseRepository examinationBrowse) : base(examinationBrowse) { }

    protected override Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken) =>
        ExaminationBrowse.ListStudentGradesAsync(cancellationToken);
}
