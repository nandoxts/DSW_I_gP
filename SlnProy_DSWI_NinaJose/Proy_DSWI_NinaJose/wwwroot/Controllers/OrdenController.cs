using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;
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
        private const string SessionKey = "Carrito";
        private readonly BDPROYVENTASContex _ctx;

        public OrdenController(BDPROYVENTASContex ctx)
            => _ctx = ctx;

        // GET /Orden/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
                return RedirectToAction("IndexCarrito", "Carrito");
            return View(cart);
        }

        // POST /Orden/Checkout

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
                return RedirectToAction("IndexCarrito", "Carrito");

            // Así obtenemos el ID del usuario autenticado de forma segura:
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Challenge(); // o RedirectToAction("Login", "Account");

            var userId = int.Parse(userIdClaim);

            var orden = new Orden
            {
                IdUsuario = userId,
                Fecha = DateTime.Now,
                Total = cart.Sum(i => i.Subtotal),
                Estado = "Pendiente"
            };

            _ctx.Ordenes.Add(orden);
            await _ctx.SaveChangesAsync();

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

            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction(nameof(Confirmation), new { id = orden.IdOrden });
        }

        // GET /Orden/Confirmation/5
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var orden = await _ctx.Ordenes
                .Include(o => o.Usuario)
                .Include(o => o.OrdenDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(o => o.IdOrden == id);

            if (orden == null) return NotFound();
            return View(orden);
        }

        // GET: /Orden/MyOrders
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MyOrders()
        {
            // Obtenemos el Id del usuario logueado
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Filtramos sólo sus órdenes
            var ordenes = await _ctx.Ordenes
                .Where(o => o.IdUsuario == userId)
                .Include(o => o.OrdenDetalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();

            // Usa la misma vista Index.cshtml
            return View("IndexOrdenes", ordenes);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllOrders()
        {
            var ordenes = await _ctx.Ordenes
                .Include(o => o.Usuario)
                .Include(o => o.OrdenDetalles).ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();

            return View("IndexOrdenesAdmin", ordenes);
        }

        // POST /Orden/ConfirmOrder/5
        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var orden = await _ctx.Ordenes.FindAsync(id);
            if (orden == null) return NotFound();

            orden.Estado = "Completada";
            await _ctx.SaveChangesAsync();

            return RedirectToAction(nameof(AllOrders));
        }
    }
}
