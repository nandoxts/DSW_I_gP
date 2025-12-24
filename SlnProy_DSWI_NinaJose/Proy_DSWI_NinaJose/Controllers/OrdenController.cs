using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;   // <-- para GetObject<>, SetObject<>
using Proy_DSWI_NinaJose.Models;
using System;
using System.IO;
using System.Net.Mail;
using System.Text;
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
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public OrdenController(BDPROYVENTASContex ctx, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _ctx = ctx;
            _config = config;
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
        public async Task<IActionResult> IndexOrdenesAdmin()
        {
            var ordenes = await _ctx.Ordenes
                .Include(o => o.Usuario)
                .Include(o => o.OrdenDetalles).ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();

            return View("IndexOrdenesAdmin", ordenes);
        }

        // GET: /Orden/DetailsMyOrder/5
        // Muestra los detalles de una orden para el propietario o para Admin
        [Authorize]
        public async Task<IActionResult> DetailsMyOrder(int id)
        {
            var orden = await _ctx.Ordenes
                .Include(o => o.OrdenDetalles).ThenInclude(d => d.Producto)
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.IdOrden == id);

            if (orden == null)
                return NotFound();

            // Si no es Admin, asegurarse que el usuario logueado sea el propietario
            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (orden.IdUsuario != userId)
                    return Forbid();
            }

            return View("DetailsMyOrder", orden);
        }

        // POST: /Orden/ConfirmOrder/5
        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var orden = await _ctx.Ordenes
                .Include(o => o.OrdenDetalles)
                .FirstOrDefaultAsync(o => o.IdOrden == id);
            if (orden == null) return NotFound();

            // Usar transacción para evitar estados intermedios
            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                // Verificar stock disponible para todos los items
                foreach (var det in orden.OrdenDetalles)
                {
                    var producto = await _ctx.Productos.FindAsync(det.IdProducto);
                    if (producto == null)
                    {
                        await tx.RollbackAsync();
                        TempData["Error"] = $"Producto (ID {det.IdProducto}) no encontrado.";
                        return RedirectToAction(nameof(IndexOrdenesAdmin));
                    }

                    if (producto.Stock < det.Cantidad)
                    {
                        await tx.RollbackAsync();
                        TempData["Error"] = $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, requerido: {det.Cantidad}.";
                        return RedirectToAction(nameof(IndexOrdenesAdmin));
                    }
                }

                // Restar stock
                foreach (var det in orden.OrdenDetalles)
                {
                    var producto = await _ctx.Productos.FindAsync(det.IdProducto);
                    producto.Stock -= det.Cantidad;
                    _ctx.Productos.Update(producto);
                }

                // Marcar orden completada
                orden.Estado = "Completada";
                _ctx.Ordenes.Update(orden);
                await _ctx.SaveChangesAsync();

                await tx.CommitAsync();

                // Enviar correo de confirmación con detalle de la orden
                try
                {
                    var emailTo = orden.Usuario?.Email;
                    if (string.IsNullOrEmpty(emailTo))
                    {
                        // intentar obtener email desde la base si no está cargado
                        var u = await _ctx.Usuarios.FindAsync(orden.IdUsuario);
                        emailTo = u?.Email;
                    }

                    if (!string.IsNullOrEmpty(emailTo))
                        await SendOrderEmailAsync(orden, emailTo);
                }
                catch
                {
                    // No fallar la confirmación por problemas de correo; registrar si se desea
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(IndexOrdenesAdmin));
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

        // Envia el email con el detalle de la orden. Si no hay configuración SMTP, guarda un HTML en wwwroot/email-logs
        private async Task SendOrderEmailAsync(Orden orden, string toEmail)
        {
            // Construir cuerpo HTML simple
            var sb = new StringBuilder();
            sb.AppendLine($"<h2>Detalle de la orden #{orden.IdOrden}</h2>");
            sb.AppendLine($"<p>Fecha: {orden.Fecha:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine($"<p>Total: {orden.Total:C}</p>");
            sb.AppendLine("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%'>");
            sb.AppendLine("<tr><th>Producto</th><th>Precio</th><th>Cantidad</th><th>Subtotal</th></tr>");
            var detalles = await _ctx.OrdenDetalles.Where(d => d.IdOrden == orden.IdOrden).Include(d => d.Producto).ToListAsync();
            foreach (var d in detalles)
            {
                var nombre = d.Producto?.Nombre ?? $"Producto {d.IdProducto}";
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(nombre)}</td><td>{d.PrecioUnitario:C}</td><td>{d.Cantidad}</td><td>{(d.PrecioUnitario * d.Cantidad):C}</td></tr>");
            }
            sb.AppendLine("</table>");

            var subject = $"Confirmación de orden #{orden.IdOrden} - MiTienda";
            var htmlBody = sb.ToString();

            // Leer configuración SMTP desde variables de entorno o appsettings
            var smtpHost = _config["SMTP:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPortRaw = _config["SMTP:Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT");
            var smtpUser = _config["SMTP:User"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPass = _config["SMTP:Pass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
            var fromEmail = _config["SMTP:From"] ?? _config["SendGrid:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM");

            if (!string.IsNullOrEmpty(smtpHost) && int.TryParse(smtpPortRaw, out var smtpPort))
            {
                using var msg = new MailMessage();
                msg.From = new MailAddress(fromEmail ?? "noreply@mitienda.local", "MiTienda");
                msg.To.Add(toEmail);
                msg.Subject = subject;
                msg.Body = htmlBody;
                msg.IsBodyHtml = true;

                using var client = new SmtpClient(smtpHost, smtpPort);
                if (!string.IsNullOrEmpty(smtpUser))
                {
                    client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                }
                client.EnableSsl = true;
                await client.SendMailAsync(msg);
            }
            else
            {
                // No hay SMTP: escribir el HTML a disco para revisión
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email-logs");
                Directory.CreateDirectory(logsDir);
                var filePath = Path.Combine(logsDir, $"order_{orden.IdOrden}_{DateTime.Now:yyyyMMddHHmmss}.html");
                await System.IO.File.WriteAllTextAsync(filePath, htmlBody, Encoding.UTF8);
            }
        }
    }
}
