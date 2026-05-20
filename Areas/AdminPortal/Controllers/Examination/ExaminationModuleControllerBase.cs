using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models.Examination;
using VEMS.Areas.AdminPortal.Services.Examination;

namespace VEMS.Areas.AdminPortal.Controllers.Examination;

public abstract class ExaminationModuleControllerBase : AdminBaseController
{
    protected readonly IExaminationBrowseRepository ExaminationBrowse;

    protected ExaminationModuleControllerBase(IExaminationBrowseRepository examinationBrowse)
    {
        ExaminationBrowse = examinationBrowse;
    }

    protected abstract Task<ExaminationListPageViewModel> GetListAsync(CancellationToken cancellationToken);

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await GetListAsync(cancellationToken);
        ViewData["Title"] = model.PageTitle;
        ViewData["PageTitle"] = model.PageTitle;
        return View("~/Areas/AdminPortal/Views/Examination/ModuleIndex.cshtml", model);
    }
}
