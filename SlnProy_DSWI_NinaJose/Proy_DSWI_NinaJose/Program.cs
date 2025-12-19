using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Proy_DSWI_NinaJose.Models;

var builder = WebApplication.CreateBuilder(args);

// 1) Configura EF Core
builder.Services.AddDbContext<BDPROYVENTASContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Autenticación por cookie
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "MiTiendaAuth";
    });

// 3) Sesiones (para carrito)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4) MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middlewares
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Ruta por defecto apunta a ProductoController.IndexProductos
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Producto}/{action=IndexProductos}/{id?}"
);

app.Run();
