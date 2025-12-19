// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Proy_DSWI_NinaJose.Models;
using System.Diagnostics;

namespace Proy_DSWI_NinaJose.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Cambiado: en lugar de return View(), redirige a Productos/IndexProductos
        public IActionResult Index()
        {
            return RedirectToAction("IndexProductos", "Productos");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
