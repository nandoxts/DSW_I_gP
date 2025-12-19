using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;   // <-- para GetObject<>, SetObject<>
using Proy_DSWI_NinaJose.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    public class OrdenController : Controller
    {
        private const string SessionKey = "Cart";
        private readonly BDPROYVENTASContex _ctx;

        public OrdenController(BDPROYVENTASContex ctx)
        {
            _ctx = ctx;
        }

        // GET: /Orden/Checkout
        // Muestra el carrito actual almacenado en sesión
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
            {
                // Si no hay items, volvemos al carrito
                return RedirectToAction("IndexCarrito", "Carrito");
            }
            return View("Checkout", cart);
        }

        // POST: /Orden/CheckoutPost
        // Procesa el pago: crea la Orden y los OrdenDetalles, limpia la sesión
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
                return RedirectToAction("IndexCarrito", "Carrito");

            // 1) Crear la Orden principal
            var orden = new Orden
            {
                // TODO: sustituye '1' por tu lógica para obtener el usuario real:
                IdUsuario = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1"),
                Fecha = DateTime.Now,
                Total = cart.Sum(i => i.Subtotal),
                Estado = "Pendiente"
            };
            _ctx.Ordenes.Add(orden);
            await _ctx.SaveChangesAsync();

            // 2) Crear cada Detalle de la Orden
            foreach (var item in cart)
            {
                _ctx.OrdenDetalles.Add(new OrdenDetalle
                {
                    IdOrden = orden.IdOrden,
                    IdProducto = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio
                });
            }
            await _ctx.SaveChangesAsync();

            // 3) Limpiar carrito en sesión
            HttpContext.Session.Remove(SessionKey);

            // 4) Redirigir a la página de confirmación
            return RedirectToAction(nameof(Confirmation), new { id = orden.IdOrden });
        }

        // GET: /Orden/Confirmation/5
        // Muestra los detalles de la orden recién creada
        public async Task<IActionResult> Confirmation(int id)
        {
            var orden = await _ctx.Ordenes
                .Include(o => o.OrdenDetalles)
                    .ThenInclude(d => d.Producto)
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.IdOrden == id);

            if (orden == null)
                return NotFound();

            return View("Confirmation", orden);
        }
    }
}
