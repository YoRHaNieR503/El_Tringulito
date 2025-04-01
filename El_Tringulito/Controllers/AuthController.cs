using System.Linq;
using System.Threading.Tasks;
using El_Tringulito.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace El_Tringulito.Controllers
{
    public class AuthController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public AuthController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string NombreUsuario, string Contrasenia)
        {
            var usuario = await _context.usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario == NombreUsuario && u.Contrasenia == Contrasenia);

            if (usuario != null)
            {
                
                HttpContext.Session.SetString("Usuario", usuario.NombreUsuario);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

