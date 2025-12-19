using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;
using Proy_DSWI_NinaJose.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    public class ProductoController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;
        public ProductoController(BDPROYVENTASContex ctx) => _ctx = ctx;

        [AllowAnonymous]
        public async Task<IActionResult> IndexProductos()
        {
            // Carga todo
            var todos = await _ctx.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Stock > 0)
                .ToListAsync();

            // Escoge, por ejemplo, los 4 primeros como destacados
            var destacados = todos
                .OrderByDescending(p => p.Precio)
                .Take(4)
                .ToList();

            // Agrupa el resto por categoría
            var resto = todos.Except(destacados);
            var porCat = resto
                .GroupBy(p => p.Categoria?.Nombre ?? "Sin categoría")
                .ToDictionary(g => g.Key, g => g.ToList());

            var vm = new ProductoCat
            {
                Destacados = destacados,
                ProductosPorCategoria = porCat
            };

            return View("~/Views/Productos/IndexProductos.cshtml", vm);
        }

        // GET: /Producto/DetailsProducto/5
        [AllowAnonymous]
        public async Task<IActionResult> DetailsProducto(int id)
        {
            var prod = await _ctx.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.IdProducto == id);
            if (prod == null) return NotFound();

            return PartialView(
                "~/Views/Productos/DetailsProducto.cshtml",
                prod
            );
        }

        // GET: /Producto/CreateProducto
        [Authorize(Roles = "Admin")]
        public IActionResult CreateProducto()
        {
            ViewBag.Categorias = new SelectList(_ctx.Categorias, "IdCategoria", "Nombre");
            return View("~/Views/Productos/CreateProducto.cshtml");
        }

        // POST: /Producto/CreateProducto
        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProducto(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = new SelectList(_ctx.Categorias, "IdCategoria", "Nombre");
                return View("~/Views/Productos/CreateProducto.cshtml", producto);
            }
            _ctx.Productos.Add(producto);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProductos));
        }

        // GET: /Producto/EditProducto/5
        public async Task<IActionResult> EditProducto(int id)
        {
            var prod = await _ctx.Productos.FindAsync(id);
            if (prod == null) return NotFound();

            ViewData["Categorias"] = new SelectList(
                await _ctx.Categorias.ToListAsync(),
                "IdCategoria", "Nombre", prod.IdCategoria);

            // Fuerza la ruta a la carpeta "Productos"
            return View("~/Views/Productos/EditProducto.cshtml", prod);
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProducto(int id, Producto producto)
        {
            if (id != producto.IdProducto)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                // Reponer las categorías para el dropdown
                ViewData["Categorias"] = new SelectList(
                    await _ctx.Categorias.ToListAsync(),
                    "IdCategoria", "Nombre",
                    producto.IdCategoria
                );
                return View("~/Views/Productos/EditProducto.cshtml", producto);
            }

            // 1. Traer de la BD la entidad original
            var prodDb = await _ctx.Productos.FindAsync(id);
            if (prodDb == null) return NotFound();

            // 2. Actualizar solo los campos permitidos
            prodDb.Nombre = producto.Nombre;
            prodDb.Precio = producto.Precio;
            prodDb.Descripcion = producto.Descripcion;
            prodDb.Stock = producto.Stock;
            prodDb.IdCategoria = producto.IdCategoria;
            // NO tocamos prodDb.ImagenUrl

            // 3. Guardar
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProductos));
        }


        // POST: /Producto/DeleteProducto/5
        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var prod = await _ctx.Productos.FindAsync(id);
            if (prod != null)
            {
                _ctx.Productos.Remove(prod);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexProductos));
        }
    }
}
