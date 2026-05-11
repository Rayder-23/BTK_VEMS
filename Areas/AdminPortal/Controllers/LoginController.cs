using Microsoft.AspNetCore.Mvc;
using VEMS.Areas.AdminPortal.Models;

namespace VEMS.Areas.AdminPortal.Controllers;

[Area("AdminPortal")]
public class LoginController : Controller
{
    public const string AdminSessionKey = "AdminUsername";

    [HttpGet]
    public IActionResult Index()
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString(AdminSessionKey)))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "AdminPortal" });
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Username == "admin" && model.Password == "admin")
        {
            HttpContext.Session.SetString(AdminSessionKey, model.Username);
            return RedirectToAction("Index", "Dashboard", new { area = "AdminPortal" });
        }

        ModelState.AddModelError(string.Empty, "Invalid Username or Password");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminSessionKey);
        return RedirectToAction("Index", "Login", new { area = "AdminPortal" });
    }
}
