using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Models;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

public class SettingsController : StudentPortalBaseController
{
    private readonly IStudentProfileRepository _profiles;

    public SettingsController(IStudentProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Change Password";
        return View(new StudentChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(StudentChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Change Password";

        var studentUid = await ResolveStudentUidAsync(_profiles, cancellationToken);
        if (studentUid is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var storedHash = await _profiles.GetPasswordHashByStudentUidAsync(studentUid.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(storedHash) ||
            !StudentPasswordHasher.VerifyPassword(model.OldPassword, storedHash))
        {
            ModelState.AddModelError(nameof(model.OldPassword), "Current password is incorrect.");
            return View(model);
        }

        if (StudentPasswordHasher.VerifyPassword(model.NewPassword, storedHash))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "New password must be different from your current password.");
            return View(model);
        }

        var newHash = StudentPasswordHasher.HashPassword(model.NewPassword);
        var updated = await _profiles.UpdatePasswordAsync(studentUid.Value, newHash, cancellationToken);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Could not update password. No portal login was found for your account.");
            return View(model);
        }

        TempData["PasswordMessage"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public IActionResult ChangeTheme()
    {
        ViewData["Title"] = "Change Theme";
        return View();
    }
}
