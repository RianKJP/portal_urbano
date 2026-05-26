using Microsoft.AspNetCore.Mvc;
using portal_urbano.Models;
using System.Diagnostics;

namespace portal_urbano.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CriarDenuncia()
        {
            return View();
        }

        public IActionResult Feed()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
