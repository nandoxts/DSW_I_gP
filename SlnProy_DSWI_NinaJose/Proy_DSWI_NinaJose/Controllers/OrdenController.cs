using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Extensions;
using Proy_DSWI_NinaJose.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdenController(
            BDPROYVENTASContex ctx,
            Microsoft.Extensions.Configuration.IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _ctx = ctx;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Orden/Checkout
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("IndexCarrito", "Carrito");
            }
            return View("Checkout", cart);
        }

        // POST: /Orden/CheckoutPost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPost()
        {
            var cart = HttpContext.Session.GetObject<List<Carrito>>(SessionKey);
            if (cart == null || !cart.Any())
                return RedirectToAction("IndexCarrito", "Carrito");

            var orden = new Orden
            {
                IdUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"),
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var ordenes = await _ctx.Ordenes
                .Where(o => o.IdUsuario == userId)
                .Include(o => o.OrdenDetalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();

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

        [Authorize]
        public async Task<IActionResult> DetailsMyOrder(int id)
        {
            var orden = await _ctx.Ordenes
                .Include(o => o.OrdenDetalles).ThenInclude(d => d.Producto)
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.IdOrden == id);

            if (orden == null)
                return NotFound();

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
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.IdOrden == id);

            if (orden == null)
            {
                TempData["Error"] = "Orden no encontrada.";
                return RedirectToAction(nameof(IndexOrdenesAdmin));
            }

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                // Verificar stock disponible
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
                    producto!.Stock -= det.Cantidad;
                    _ctx.Productos.Update(producto);
                }

                // Marcar orden completada
                orden.Estado = "Completada";
                _ctx.Ordenes.Update(orden);
                await _ctx.SaveChangesAsync();

                await tx.CommitAsync();

                // Enviar correo de confirmación
                string emailStatus = "";
                try
                {
                    var emailTo = orden.Usuario?.Email;
                    if (string.IsNullOrEmpty(emailTo))
                    {
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

                        var emailResult = await SendOrderEmailWithSendGridAsync(orden, emailTo, userName);
                        emailStatus = emailResult.success
                            ? $" ✉️ Email enviado a {emailTo}"
                            : $" ⚠️ Email no enviado: {emailResult.message}";
                    }
                    else
                    {
                        emailStatus = " ⚠️ No se pudo enviar email (cliente sin correo)";
                    }
                }
                catch (Exception ex)
                {
                    emailStatus = $" ⚠️ Error al enviar email: {ex.Message}";
                }

                TempData["Success"] = $"Orden #{orden.IdOrden} confirmada exitosamente.{emailStatus}";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Error al confirmar la orden: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexOrdenesAdmin));
        }

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

        // ============================================
        // SENDGRID EMAIL SERVICE
        // ============================================

        private async Task<(bool success, string message)> SendOrderEmailWithSendGridAsync(Orden orden, string toEmail, string customerName)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"] ?? "MiTienda";

            // Verificar si SendGrid está configurado
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                // Fallback: guardar HTML en disco
                await SaveEmailToFileAsync(orden, toEmail, customerName);
                return (false, "SendGrid no configurado. Email guardado en archivo.");
            }

            // Obtener detalles de la orden
            var detalles = await _ctx.OrdenDetalles
                .Where(d => d.IdOrden == orden.IdOrden)
                .Include(d => d.Producto)
                .ToListAsync();

            // Construir el HTML del email
            var htmlContent = BuildOrderEmailHtml(orden, detalles, customerName);

            // Preparar el payload de SendGrid
            var sendGridPayload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = toEmail, name = customerName } }
                    }
                },
                from = new { email = fromEmail, name = fromName },
                subject = $"Pedido #{orden.IdOrden} Confirmado - {fromName}",
                content = new[]
                {
                    new { type = "text/html", value = htmlContent }
                }
            };

            var json = JsonSerializer.Serialize(sendGridPayload);

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.Timeout = TimeSpan.FromSeconds(30);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Email enviado correctamente");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    
                    // Guardar en archivo como fallback
                    await SaveEmailToFileAsync(orden, toEmail, customerName);
                    
                    return (false, $"SendGrid error {(int)response.StatusCode}: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                await SaveEmailToFileAsync(orden, toEmail, customerName);
                return (false, $"Error de conexión: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                await SaveEmailToFileAsync(orden, toEmail, customerName);
                return (false, "Timeout al conectar con SendGrid");
            }
        }

        private async Task SaveEmailToFileAsync(Orden orden, string toEmail, string customerName)
        {
            var detalles = await _ctx.OrdenDetalles
                .Where(d => d.IdOrden == orden.IdOrden)
                .Include(d => d.Producto)
                .ToListAsync();

            var htmlBody = BuildOrderEmailHtml(orden, detalles, customerName);

            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "email-logs");
            Directory.CreateDirectory(logsDir);
            
            var fileName = $"order_{orden.IdOrden}_{DateTime.Now:yyyyMMddHHmmss}.html";
            var filePath = Path.Combine(logsDir, fileName);
            
            // Agregar metadata al inicio del archivo
            var metadata = $"<!--\nPara: {toEmail}\nCliente: {customerName}\nOrden: #{orden.IdOrden}\nFecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n-->\n";
            await System.IO.File.WriteAllTextAsync(filePath, metadata + htmlBody, Encoding.UTF8);
        }

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

            // Productos
            foreach (var d in detalles)
            {
                var nombre = d.Producto?.Nombre ?? $"Producto {d.IdProducto}";
                var subtotal = d.PrecioUnitario * d.Cantidad;

                sb.AppendLine($@"
                                <tr>
                                    <td style=""padding: 16px; border-bottom: 1px solid #f3f4f6;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                            <tr>
                                                <td width=""50%"">
                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"">
                                                        <tr>
                                                            <td style=""width: 40px; height: 40px; background-color: #eef2ff; border-radius: 8px; text-align: center; vertical-align: middle;"">
                                                                <!-- Modern SVG icon for product/box -->
                                                                <svg width=""28"" height=""28"" viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""vertical-align: middle;"">
                                                                    <rect x=""3"" y=""7"" width=""18"" height=""13"" rx=""3"" fill=""#6366f1""/>
                                                                    <rect x=""7"" y=""3"" width=""10"" height=""4"" rx=""2"" fill=""#a5b4fc""/>
                                                                    <rect x=""9"" y=""11"" width=""6"" height=""2"" rx=""1"" fill=""#fff""/>
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
    }
}