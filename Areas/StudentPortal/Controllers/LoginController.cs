using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.StudentPortal.Models;
using VEMS.Areas.StudentPortal.Services;

namespace VEMS.Areas.StudentPortal.Controllers;

[Area("StudentPortal")]
public class LoginController : Controller
{
    private readonly IStudentLoginRepository _studentLoginRepository;

    public LoginController(IStudentLoginRepository studentLoginRepository)
    {
        _studentLoginRepository = studentLoginRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "StudentPortal" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new StudentLoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StudentLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var student = await _studentLoginRepository.ValidateCredentialsAsync(model.Username, model.Password);
        if (student is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, student.Uid.ToString()),
            new(ClaimTypes.Name, student.Username),
            new(ClaimTypes.Role, student.Role)
        };

        if (!string.IsNullOrWhiteSpace(student.StudentId))
        {
            claims.Add(new Claim("StudentId", student.StudentId));
        }

        claims.Add(new Claim("LoginAt", DateTime.UtcNow.ToString("o")));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 : 8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "StudentPortal" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login", new { area = "StudentPortal" });
    }
}
