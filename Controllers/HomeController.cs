using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VEMS.Models;

namespace VEMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>Site root: avoid Razor view resolution failures on some hosts; send users to the admin portal.</summary>
        public IActionResult Index() => Redirect("/adminportal");

        public IActionResult Privacy() => View("~/Views/Home/Privacy.cshtml");

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
