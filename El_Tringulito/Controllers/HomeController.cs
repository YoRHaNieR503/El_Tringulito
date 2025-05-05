using System;
using System.Linq;
using System.Threading.Tasks;
using El_Tringulito.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace El_Tringulito.Controllers
{
    [Authorize(Roles = "admin")]
    public class HomeController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public HomeController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["CombosCount"] = await _context.combos.CountAsync();
                ViewData["MesasCount"] = await _context.mesas.CountAsync();
                ViewData["OrdenesCount"] = await _context.ordenes.CountAsync();
                ViewData["PlatosCount"] = await _context.platos.CountAsync();

                ViewData["OrdenesRecientes"] = await _context.ordenes
                    .OrderByDescending(o => o.fecha)
                    .Take(5)
                    .ToListAsync();

                ViewData["TotalVentas"] = await _context.ordenes.SumAsync(o => o.total ?? 0);
                ViewData["MesasDisponibles"] = await _context.mesas.CountAsync(m => m.estado == "Disponible");
                ViewData["MesasOcupadas"] = await _context.mesas.CountAsync(m => m.estado == "Ocupada");

                return View();
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
