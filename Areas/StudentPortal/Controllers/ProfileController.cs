using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Models;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

public sealed class ProfileController : StudentPortalBaseController
{
    private readonly IStudentProfileRepository _profiles;

    public ProfileController(IStudentProfileRepository profiles)
    {
        _profiles = profiles;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "My Profile";

        var page = await BuildPageModelAsync(cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        return View(page);
    }

    private async Task<StudentProfilePageViewModel?> BuildPageModelAsync(CancellationToken cancellationToken)
    {
        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return null;
        }

        var profile = await _profiles.GetByStudentUidAsync(studentUid.Value, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        return new StudentProfilePageViewModel { Profile = profile };
    }
}
