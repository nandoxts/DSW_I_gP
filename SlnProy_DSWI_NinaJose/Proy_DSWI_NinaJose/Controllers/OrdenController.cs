using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;   // <-- para GetObject<>, SetObject<>
using Proy_DSWI_NinaJose.Models;
using System;
using System.IO;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
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

        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("IndexCarrito", "Carrito");
            }
            return View("Checkout", cart);
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
                return RedirectToAction("IndexCarrito", "Carrito");

            var orden = new Orden
            {
                IdUsuario = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "1"),
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
                    {
                        var userName = orden.Usuario?.Nombre;
                        if (string.IsNullOrEmpty(userName))
                        {
                            var u = await _ctx.Usuarios.FindAsync(orden.IdUsuario);
                            userName = u?.Nombre ?? "Cliente";
                        }
                        await SendOrderEmailAsync(orden, emailTo, userName);
                    }
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

        // Genera el HTML del email con diseño moderno
        private string BuildOrderEmailHtml(Orden orden, List<OrdenDetalle> detalles, string customerName)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Confirmación de Orden</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f3f4f6; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 40px 40px 30px; text-align: center;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td align=""center"">
                                        <!-- Logo Icon -->
                                        <div style=""width: 60px; height: 60px; background-color: rgba(255,255,255,0.2); border-radius: 12px; margin: 0 auto 16px; line-height: 60px;"">
                                            <span style=""font-size: 28px;"">🛍️</span>
                                        </div>
                                        <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">¡Pedido Confirmado!</h1>
                                        <p style=""color: rgba(255,255,255,0.9); margin: 12px 0 0; font-size: 16px;"">Gracias por tu compra en MiTienda</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Success Badge -->
                    <tr>
                        <td style=""padding: 30px 40px 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td align=""center"">
                                        <div style=""display: inline-block; background-color: #ecfdf5; border-radius: 50px; padding: 10px 24px;"">
                                            <span style=""color: #059669; font-size: 14px; font-weight: 600;"">✓ Orden procesada exitosamente</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Greeting -->
                    <tr>
                        <td style=""padding: 30px 40px 20px;"">
                            <p style=""color: #374151; margin: 0; font-size: 16px; line-height: 1.6;"">
                                Hola <strong style=""color: #111827;"">" + System.Net.WebUtility.HtmlEncode(customerName) + @"</strong>,
                            </p>
                            <p style=""color: #6b7280; margin: 12px 0 0; font-size: 15px; line-height: 1.6;"">
                                Tu pedido ha sido confirmado y está siendo procesado. A continuación encontrarás el detalle de tu compra.
                            </p>
                        </td>
                    </tr>

                    <!-- Order Info Card -->
                    <tr>
                        <td style=""padding: 0 40px 20px;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f9fafb; border-radius: 12px; border: 1px solid #e5e7eb;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td width=""50%"" style=""padding-right: 10px;"">
                                                    <p style=""color: #9ca3af; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 4px;"">Número de Orden</p>
                                                    <p style=""color: #111827; font-size: 20px; font-weight: 700; margin: 0;"">#" + orden.IdOrden + @"</p>
                                                </td>
                                                <td width=""50%"" style=""padding-left: 10px; text-align: right;"">
                                                    <p style=""color: #9ca3af; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; margin: 0 0 4px;"">Fecha</p>
                                                    <p style=""color: #111827; font-size: 16px; font-weight: 600; margin: 0;"">" + orden.Fecha.ToString("dd MMM yyyy, HH:mm") + @"</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Products Section Title -->
                    <tr>
                        <td style=""padding: 10px 40px 15px;"">
                            <p style=""color: #111827; font-size: 16px; font-weight: 600; margin: 0;"">Detalle de productos</p>
                        </td>
                    </tr>

                    <!-- Products Table -->
                    <tr>
                        <td style=""padding: 0 40px 20px;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""border: 1px solid #e5e7eb; border-radius: 12px; overflow: hidden;"">
                                <!-- Table Header -->
                                <tr>
                                    <td style=""background-color: #f9fafb; padding: 14px 16px; border-bottom: 1px solid #e5e7eb;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td width=""50%"" style=""color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">Producto</td>
                                                <td width=""15%"" align=""center"" style=""color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">Cant.</td>
                                                <td width=""17%"" align=""right"" style=""color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">Precio</td>
                                                <td width=""18%"" align=""right"" style=""color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">Subtotal</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>");

            // Products rows
            foreach (var d in detalles)
            {
                var nombre = d.Producto?.Nombre ?? $"Producto {d.IdProducto}";
                var subtotal = d.PrecioUnitario * d.Cantidad;
                
                sb.AppendLine($@"
                                <!-- Product Row -->
                                <tr>
                                    <td style=""padding: 16px; border-bottom: 1px solid #f3f4f6;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td width=""50%"">
                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"">
                                                        <tr>
                                                            <td style=""width: 40px; height: 40px; background-color: #eef2ff; border-radius: 8px; text-align: center; vertical-align: middle;"">
                                                                <!-- Modern SVG Icon -->
                                                                <svg width=""24"" height=""24"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""vertical-align:middle;"">
                                                                    <rect x=""3"" y=""7"" width=""18"" height=""13"" rx=""3"" fill=""#6366f1""/>
                                                                    <rect x=""1"" y=""3"" width=""22"" height=""6"" rx=""2"" fill=""#a5b4fc""/>
                                                                    <rect x=""7"" y=""11"" width=""10"" height=""2"" rx=""1"" fill=""#fff""/>
                                                                </svg>
                                                            </td>
                                                            <td style=""padding-left: 12px;"">
                                                                <p style=""color: #111827; font-size: 14px; font-weight: 500; margin: 0;"">{System.Net.WebUtility.HtmlEncode(nombre)}</p>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                                <td width=""15%"" align=""center"" style=""color: #6b7280; font-size: 14px;"">{d.Cantidad}</td>
                                                <td width=""17%"" align=""right"" style=""color: #6b7280; font-size: 14px;"">S/ {d.PrecioUnitario:F2}</td>
                                                <td width=""18%"" align=""right"" style=""color: #111827; font-size: 14px; font-weight: 600;"">S/ {subtotal:F2}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>");
            }

            sb.AppendLine($@"
                            </table>
                        </td>
                    </tr>

                    <!-- Total Section -->
                    <tr>
                        <td style=""padding: 0 40px 30px;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); border-radius: 12px;"">
                                <tr>
                                    <td style=""padding: 20px 24px;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td>
                                                    <p style=""color: rgba(255,255,255,0.8); font-size: 14px; margin: 0;"">Total a pagar</p>
                                                </td>
                                                <td align=""right"">
                                                    <p style=""color: #ffffff; font-size: 28px; font-weight: 700; margin: 0; letter-spacing: -0.5px;"">S/ {orden.Total:F2}</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                

                    <!-- CTA Button -->
                    <tr>
                        <td style=""padding: 0 40px 30px;"" align=""center"">
                            <a href=""#"" style=""display: inline-block; background-color: #4f46e5; color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-size: 15px; font-weight: 600;"">Ver mis pedidos</a>
                        </td>
                    </tr>

                    <!-- Divider -->
                    <tr>
                        <td style=""padding: 0 40px;"">
                            <div style=""height: 1px; background-color: #e5e7eb;""></div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 30px 40px; text-align: center;"">
                            <p style=""color: #9ca3af; font-size: 13px; margin: 0 0 8px;"">¿Tienes alguna pregunta?</p>
                            <p style=""color: #6b7280; font-size: 13px; margin: 0 0 20px;"">Contáctanos en <a href=""mailto:soporte@mitienda.com"" style=""color: #4f46e5; text-decoration: none; font-weight: 500;"">soporte@mitienda.com</a></p>
                            
                
                            <p style=""color: #9ca3af; font-size: 12px; margin: 20px 0 0;"">© {DateTime.Now.Year} MiTienda. Todos los derechos reservados.</p>
                        </td>
                    </tr>

                </table>

                <!-- Footer Text -->
                <table role=""presentation"" width=""600"" cellspacing=""0"" cellpadding=""0"">
                    <tr>
                        <td style=""padding: 20px 40px; text-align: center;"">
                            <p style=""color: #9ca3af; font-size: 11px; margin: 0; line-height: 1.5;"">
                                Este correo fue enviado a {System.Net.WebUtility.HtmlEncode(customerName)} porque realizaste una compra en MiTienda.<br>
                                Si no reconoces esta compra, por favor contáctanos inmediatamente.
                            </p>
                        </td>
                    </tr>
                </table>

            </td>
        </tr>
    </table>
</body>
</html>");

            return sb.ToString();
        }

        // Envia el email con el detalle de la orden. Si no hay configuración SMTP, guarda un HTML en wwwroot/email-logs
        private async Task SendOrderEmailAsync(Orden orden, string toEmail, string customerName)
        {
            // Obtener detalles de la orden
            var detalles = await _ctx.OrdenDetalles
                .Where(d => d.IdOrden == orden.IdOrden)
                .Include(d => d.Producto)
                .ToListAsync();

            // Construir el HTML del email
            var htmlBody = BuildOrderEmailHtml(orden, detalles, customerName);
            var subject = $"Pedido #{orden.IdOrden} Confirmado - MiTienda";

            // Leer configuración SMTP desde variables de entorno o appsettings
            var smtpHost = _config["SMTP:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPortRaw = _config["SMTP:Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT");
            var smtpUser = _config["SMTP:User"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPass = _config["SMTP:Pass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
            var fromEmail = _config["SMTP:From"] ?? _config["SendGrid:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM");

            // Prefer SendGrid API if ApiKey is configured
            var sendGridApiKey = _config["SendGrid:ApiKey"] ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                var client = new SendGridClient(sendGridApiKey);
                var from = new EmailAddress(fromEmail ?? "noreply@mitienda.local", _config["SendGrid:FromName"] ?? "MiTienda");
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", ""), htmlBody);
                var response = await client.SendEmailAsync(msg);
                // If SendGrid succeeds, return early. Otherwise fall back to SMTP or file.
                if (response.IsSuccessStatusCode)
                    return;
            }

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