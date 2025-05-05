using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using El_Tringulito.Models;
using El_Tringulito.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace El_Tringulito.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public AuthController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string NombreUsuario, string Contrasenia, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(NombreUsuario) || string.IsNullOrEmpty(Contrasenia))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña";
                return View();
            }

            try
            {
                var usuario = await _context.usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == NombreUsuario);

                if (usuario != null && PasswordHelper.VerifyPassword(usuario.Contrasenia, Contrasenia))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim(ClaimTypes.Role, usuario.Rol) // Rol agregado como claim
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Redirección según rol
                    return usuario.Rol switch
                    {
                        "admin" => RedirectToAction("Index", "Home"),
                        "mesero" => RedirectToAction("Index", "MesasMesero"),
                        "cocina" => RedirectToAction("Index", "Cocina"),
                        _ => RedirectToAction("Login")
                    };
                }

                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View();
            }
            catch
            {
                ViewBag.Error = "Ocurrió un error al iniciar sesión";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _context.usuarios.FindAsync(int.Parse(userId));

            if (usuario == null) return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Las contraseñas nuevas no coinciden";
                return View();
            }

            if (newPassword.Length < 8)
            {
                ViewBag.Error = "La nueva contraseña debe tener al menos 8 caracteres";
                return View();
            }

            if (!PasswordHelper.VerifyPassword(usuario.Contrasenia, currentPassword))
            {
                ViewBag.Error = "La contraseña actual es incorrecta";
                return View();
            }

            usuario.Contrasenia = PasswordHelper.HashPassword(newPassword);
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Contraseña cambiada exitosamente";
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
