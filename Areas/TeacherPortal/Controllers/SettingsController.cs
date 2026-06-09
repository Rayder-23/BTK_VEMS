using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Services;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

public sealed class SettingsController : TeacherPortalBaseController
{
    private readonly ITeacherAccountRepository _accounts;

    public SettingsController(ITeacherAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public IActionResult Index() => RedirectToAction(nameof(GeneralSettings));

    [HttpGet]
    public IActionResult GeneralSettings()
    {
        ViewData["Title"] = "General Settings";
        return View("ChangeTheme");
    }

    [HttpGet]
    public IActionResult UserSettings()
    {
        ViewData["Title"] = "User Settings";
        return View();
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Change Password";
        return View(new TeacherChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        TeacherChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Change Password";

        var loginUid = ResolveLoginUid();
        if (loginUid is null)
        {
            return RedirectToAction("Index", "Login", new { area = "TeacherPortal" });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var storedHash = await _accounts.GetPasswordHashByLoginUidAsync(loginUid.Value, cancellationToken);
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
        var updated = await _accounts.UpdatePasswordAsync(loginUid.Value, newHash, cancellationToken);
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
