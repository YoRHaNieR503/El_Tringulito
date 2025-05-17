using El_Tringulito.Hubs;
using El_Tringulito.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ElTriangulitoDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("elTriangulitoDbConnection")));

// Autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.IsEssential = true;
        options.Cookie.MaxAge = null;
        options.SlidingExpiration = false;
    });

// Requiere autenticación por defecto
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.Use(async (context, next) =>
{
    var user = context.User;
    var path = context.Request.Path.Value?.ToLower();

    if (user.Identity?.IsAuthenticated == true)
    {
        var isAdmin = user.IsInRole("admin");
        var isMesero = user.IsInRole("mesero");
        var isCocina = user.IsInRole("cocina");

        if (!isAdmin)
        {
            if (isMesero && !path.StartsWith("/mesasmesero"))
            {
                context.Response.Redirect("/MesasMesero");
                return;
            }

            if (isCocina && !path.StartsWith("/cocina"))
            {
                context.Response.Redirect("/Cocina");
                return;
            }
        }
    }

    await next.Invoke();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<CocinaHub>("/cocinaHub");

app.Run();
