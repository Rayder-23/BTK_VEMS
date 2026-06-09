using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.TeacherPortal.Models;
using VEMS.Areas.TeacherPortal.Services;

namespace VEMS.Areas.TeacherPortal.Controllers;

[Area("TeacherPortal")]
public sealed class LoginController : Controller
{
    private readonly ITeacherLoginRepository _teacherLoginRepository;

    public LoginController(ITeacherLoginRepository teacherLoginRepository)
    {
        _teacherLoginRepository = teacherLoginRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var teacherAuth = await HttpContext.AuthenticateAsync(TeacherPortalAuth.Scheme);
        if (teacherAuth.Succeeded)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "TeacherPortal" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new TeacherLoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        TeacherLoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _teacherLoginRepository.ValidateCredentialsAsync(
            model.Username,
            model.Password,
            cancellationToken);

        if (result.FailureReason is not null)
        {
            var message = result.FailureReason switch
            {
                TeacherLoginFailureReason.NotTeacherRole =>
                    "You do not have teacher role.",
                TeacherLoginFailureReason.InactiveAccount =>
                    "Your account is not active. Contact administration.",
                _ => "Invalid username or password."
            };

            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        var teacher = result.User!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, teacher.LoginUid.ToString()),
            new(ClaimTypes.Name, teacher.DisplayName),
            new(ClaimTypes.Role, teacher.Role),
            new("EmployeeUid", teacher.EmployeeUid.ToString()),
            new("EmployeeId", teacher.EmployeeCode),
            new("Username", teacher.Username),
            new("LoginAt", DateTime.UtcNow.ToString("o"))
        };

        var identity = new ClaimsIdentity(claims, TeacherPortalAuth.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 : 8)
        };

        await HttpContext.SignInAsync(
            TeacherPortalAuth.Scheme,
            principal,
            authenticationProperties);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "TeacherPortal" });
    }

    [Authorize(AuthenticationSchemes = TeacherPortalAuth.Scheme)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(TeacherPortalAuth.Scheme);
        return RedirectToAction("Index", "Login", new { area = "TeacherPortal" });
    }
}
