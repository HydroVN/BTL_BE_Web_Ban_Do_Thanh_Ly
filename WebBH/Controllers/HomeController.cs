using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBH.Models;

namespace WebBH.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult About()
<<<<<<< HEAD
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
=======
>>>>>>> origin/main
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }
    }
}
