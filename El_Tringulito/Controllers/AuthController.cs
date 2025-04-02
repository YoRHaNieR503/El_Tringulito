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
            // Si ya está autenticado, redirigir al home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

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
                var usuario = await _context.usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == NombreUsuario);

                if (usuario != null && PasswordHelper.VerifyPassword(usuario.Contrasenia, Contrasenia))
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim("FullName", usuario.NombreUsuario)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Usuario o contraseña incorrectos";
                return View();
            }
            catch (Exception ex)
            {
                // Loggear el error
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
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var usuario = await _context.usuarios.FindAsync(int.Parse(userId));

                if (usuario == null)
                {
                    return RedirectToAction("Login");
                }

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
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error al cambiar la contraseña";
                return View();
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}