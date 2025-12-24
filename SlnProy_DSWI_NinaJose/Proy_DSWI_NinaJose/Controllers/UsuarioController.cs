// Controllers/UsuarioController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuarioController : Controller
    {
        private readonly BDPROYVENTASContex _ctx = null!;
        public UsuarioController(BDPROYVENTASContex ctx) => _ctx = ctx;

        // GET /Usuario
        public async Task<IActionResult> IndexUsuario()
        {
            var usuarios = await _ctx.Usuarios.AsNoTracking().ToListAsync();
            return View(usuarios);
        }

        // GET /Usuario/Details/{id}
        public async Task<IActionResult> DetailsUsuario(int? id)
        {
            if (id == null) return NotFound();
            var u = await _ctx.Usuarios
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u == null) return NotFound();
            return View(u);
        }

        // GET /Usuario/Edit/{id}
        public async Task<IActionResult> EditUsuario(int? id)
        {
            if (id == null) return NotFound();
            var u = await _ctx.Usuarios.FindAsync(id);
            if (u == null) return NotFound();

            ViewData["Roles"] = new SelectList(
                new[] {
                    new { Value = "Cliente", Text = "Usuario" },
                    new { Value = "Admin",   Text = "Administrador" }
                },
                "Value", "Text", u.Rol
            );
            return View(u);
        }

        // POST /Usuario/Edit/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsuario(int id, int IdUsuario, string Nombre, string Email, string Rol)
        {
            if (id != IdUsuario) return BadRequest();

            // Validación manual para evitar validar propiedades no enviadas (p.ej. PasswordHash)
            if (string.IsNullOrWhiteSpace(Nombre)) ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(Email)) ModelState.AddModelError("Email", "El email es obligatorio.");

            var usuarioEntity = await _ctx.Usuarios.FindAsync(id);
            if (usuarioEntity == null) return NotFound();

            // Verificar email único (si cambió)
            if (!string.Equals(usuarioEntity.Email, Email, System.StringComparison.OrdinalIgnoreCase))
            {
                if (await _ctx.Usuarios.AnyAsync(u => u.Email == Email && u.IdUsuario != id))
                {
                    ModelState.AddModelError("Email", "El email ya está registrado para otro usuario.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["Roles"] = new SelectList(
                    new[] {
                        new { Value = "Cliente", Text = "Usuario" },
                        new { Value = "Admin",   Text = "Administrador" }
                    },
                    "Value", "Text", usuarioEntity.Rol
                );

                // Preparar modelo para la vista (mostrar los valores intentados)
                usuarioEntity.Nombre = Nombre;
                usuarioEntity.Email = Email;
                usuarioEntity.Rol = Rol;
                return View(usuarioEntity);
            }

            // Actualizar sólo los campos editables
            usuarioEntity.Nombre = Nombre;
            usuarioEntity.Email = Email;
            usuarioEntity.Rol = Rol;

            try
            {
                _ctx.Update(usuarioEntity);
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _ctx.Usuarios.AnyAsync(e => e.IdUsuario == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(IndexUsuario));
        }

        // GET /Usuario/Delete/{id}
        public async Task<IActionResult> DeleteUsuario(int? id)
        {
            if (id == null) return NotFound();
            var u = await _ctx.Usuarios
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u == null) return NotFound();
            return View(u);
        }

        // POST /Usuario/Delete/{id}
        [HttpPost, ActionName("DeleteUsuario"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsuarioConfirmed(int id)
        {
            var u = await _ctx.Usuarios.FindAsync(id);
            if (u != null)
            {
                _ctx.Usuarios.Remove(u);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexUsuario));
        }
    }
}
