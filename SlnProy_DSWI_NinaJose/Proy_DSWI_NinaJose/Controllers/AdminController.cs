using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;

        public AdminController(BDPROYVENTASContex ctx)
        {
            _ctx = ctx;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var hoy = DateTime.Now.Date;
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Estadísticas
            var totalUsuarios = await _ctx.Usuarios.CountAsync();
            var totalProductos = await _ctx.Productos.CountAsync();
            var totalOrdenes = await _ctx.Ordenes.CountAsync();
            var ventasHoy = await _ctx.Ordenes
                .Where(o => o.Fecha.Date == hoy)
                .SumAsync(o => o.Total);
            var ventasMes = await _ctx.Ordenes
                .Where(o => o.Fecha >= inicioMes)
                .SumAsync(o => o.Total);

            // Últimas órdenes
            var ultimasOrdenes = await _ctx.Ordenes
                .Include(o => o.Usuario)
                .OrderByDescending(o => o.Fecha)
                .Take(5)
                .ToListAsync();

            // Productos más vendidos
            var productosPopulares = await _ctx.Ordenes
                .SelectMany(o => o.OrdenDetalles)
                .GroupBy(od => od.Producto.Nombre)
                .Select(g => new { Producto = g.Key, Cantidad = g.Sum(od => od.Cantidad) })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.TotalProductos = totalProductos;
            ViewBag.TotalOrdenes = totalOrdenes;
            ViewBag.VentasHoy = ventasHoy;
            ViewBag.VentasMes = ventasMes;
            ViewBag.UltimasOrdenes = ultimasOrdenes;
            ViewBag.ProductosPopulares = productosPopulares;

            return View();
        }
    }
}
