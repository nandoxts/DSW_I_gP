using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    public class AccountController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;
        public AccountController(BDPROYVENTASContex ctx) => _ctx = ctx;

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Email)) errors.Add("El email es obligatorio.");
            if (string.IsNullOrWhiteSpace(Password)) errors.Add("La contraseña es obligatoria.");

            if (errors.Count == 0)
            {
                var user = await _ctx.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == Email && u.PasswordHash == Password);
                if (user == null)
                    errors.Add("Credenciales inválidas.");
                else
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
                        new Claim(ClaimTypes.Name,           user.Nombre),
                        new Claim(ClaimTypes.Role,           user.Rol)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity)
                    );
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, errors });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(string Nombre, string Email, string Password, string ConfirmPassword)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Nombre)) errors.Add("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(Email)) errors.Add("El email es obligatorio.");
            if (string.IsNullOrWhiteSpace(Password)) errors.Add("La contraseña es obligatoria.");
            if (Password != ConfirmPassword) errors.Add("Las contraseñas no coinciden.");

            if (errors.Count == 0)
            {
                if (await _ctx.Usuarios.AnyAsync(u => u.Email == Email))
                    errors.Add("El email ya está registrado.");
                else
                {
                    var user = new Usuario
                    {
                        Nombre = Nombre,
                        Email = Email,
                        PasswordHash = Password,
                        Rol = "Cliente"
                    };
                    _ctx.Usuarios.Add(user);
                    await _ctx.SaveChangesAsync();

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
                        new Claim(ClaimTypes.Name,           user.Nombre),
                        new Claim(ClaimTypes.Role,           user.Rol)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity)
                    );
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false, errors });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("IndexProductos", "Producto");
        }
    }
}
