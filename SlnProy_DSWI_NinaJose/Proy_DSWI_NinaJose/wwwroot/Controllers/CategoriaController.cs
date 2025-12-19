using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriaController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;
        public CategoriaController(BDPROYVENTASContex ctx) => _ctx = ctx;

        // GET: /Categoria
        public async Task<IActionResult> Index()
        {
            var cats = await _ctx.Categorias.ToListAsync();
            return View("~/Views/Categoria/Index.cshtml", cats);
        }

        // GET: /Categoria/Create
        public IActionResult Create() => View("~/Views/Categoria/Create.cshtml");

        // POST: /Categoria/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria cat)
        {
            if (!ModelState.IsValid) return View(cat);
            _ctx.Categorias.Add(cat);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categoria/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _ctx.Categorias.FindAsync(id);
            if (cat == null) return NotFound();
            return View("~/Views/Categoria/Edit.cshtml", cat);
        }

        // POST: /Categoria/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria updated)
        {
            if (id != updated.IdCategoria) return BadRequest();
            if (!ModelState.IsValid) return View(updated);
            _ctx.Entry(updated).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Categoria/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _ctx.Categorias.FindAsync(id);
            if (cat != null)
            {
                _ctx.Categorias.Remove(cat);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
