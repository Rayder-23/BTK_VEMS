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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(StudentChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "My Profile";

        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        var profile = await _profiles.GetByStudentUidAsync(studentUid.Value, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", new StudentProfilePageViewModel
            {
                Profile = profile,
                ChangePassword = model
            });
        }

        var storedHash = await _profiles.GetPasswordHashByStudentUidAsync(studentUid.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(storedHash) ||
            !StudentPasswordHasher.VerifyPassword(model.OldPassword, storedHash))
        {
            ModelState.AddModelError(nameof(model.OldPassword), "Current password is incorrect.");
            return View("Index", new StudentProfilePageViewModel
            {
                Profile = profile,
                ChangePassword = model
            });
        }

        if (StudentPasswordHasher.VerifyPassword(model.NewPassword, storedHash))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "New password must be different from your current password.");
            return View("Index", new StudentProfilePageViewModel
            {
                Profile = profile,
                ChangePassword = model
            });
        }

        var newHash = StudentPasswordHasher.HashPassword(model.NewPassword);
        var updated = await _profiles.UpdatePasswordAsync(studentUid.Value, newHash, cancellationToken);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Could not update password. No portal login was found for your account.");
            return View("Index", new StudentProfilePageViewModel
            {
                Profile = profile,
                ChangePassword = model
            });
        }

        TempData["PasswordMessage"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(Index));
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
