using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Controllers;

[Area("AdminPortal")]
public class LoginController : Controller
{
    public const string AdminSessionKey = "AdminUsername";

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        var adminAuth = await HttpContext.AuthenticateAsync(AdminPortalAuth.Scheme);
        if (adminAuth.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Username == "admin" && model.Password == "admin")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, model.Username),
                new(ClaimTypes.Role, "Admin"),
                new("LoginAt", DateTime.UtcNow.ToString("o"))
            };

            var identity = new ClaimsIdentity(claims, AdminPortalAuth.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 * 7 : 8)
            };

            await HttpContext.SignInAsync(
                AdminPortalAuth.Scheme,
                principal,
                authenticationProperties);

            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid Username or Password");
        return View(model);
    }

    [Authorize(AuthenticationSchemes = AdminPortalAuth.Scheme)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AdminPortalAuth.Scheme);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "AdminPortal" });
    }
}
