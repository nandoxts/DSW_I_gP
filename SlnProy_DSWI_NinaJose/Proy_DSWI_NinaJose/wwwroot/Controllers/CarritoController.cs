using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;
using Proy_DSWI_NinaJose.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class CarritoController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;
        const string SESSION_KEY = "Carrito";

        public CarritoController(BDPROYVENTASContex ctx) => _ctx = ctx;

        [HttpGet]
        public IActionResult IndexCarrito()
        {
            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SESSION_KEY) ?? new List<Carrito>();
            return View(carrito);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int cantidad)
        {
            var prod = await _ctx.Productos.FindAsync(id);
            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SESSION_KEY) ?? new List<Carrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);
            if (item != null) item.Cantidad += cantidad;
            else carrito.Add(new Carrito
            {
                ProductoId = id,
                Nombre = prod.Nombre,
                Precio = prod.Precio,
                Cantidad = cantidad,
                ImagenUrl = prod.ImagenUrl
            });
            HttpContext.Session.SetObject(SESSION_KEY, carrito);
            return Json(new { success = true, newCount = carrito.Sum(c => c.Cantidad) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int cantidad)
        {
            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SESSION_KEY) ?? new List<Carrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);
            if (item != null)
            {
                item.Cantidad = cantidad;
                if (item.Cantidad <= 0) carrito.Remove(item);
            }
            HttpContext.Session.SetObject(SESSION_KEY, carrito);
            return Json(new
            {
                success = true,
                newCount = carrito.Sum(c => c.Cantidad),
                newSubtotal = item?.Subtotal ?? 0,
                newTotal = carrito.Sum(c => c.Subtotal)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SESSION_KEY) ?? new List<Carrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);
            if (item != null) carrito.Remove(item);
            HttpContext.Session.SetObject(SESSION_KEY, carrito);
            return Json(new
            {
                success = true,
                newCount = carrito.Sum(c => c.Cantidad),
                newTotal = carrito.Sum(c => c.Subtotal)
            });
        }
    }
}
