// HomeController.cs actualizado
using System;
using System.Linq;
using System.Threading.Tasks;
using El_Tringulito.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

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
                ViewData["PromocionesCount"] = await _context.promociones.CountAsync();
                ViewData["TotalVentas"] = await _context.ordenes.SumAsync(o => o.total ?? 0);
                ViewData["MesasDisponibles"] = await _context.mesas.CountAsync(m => m.estado == "Libre");
                ViewData["MesasOcupadas"] = await _context.mesas.CountAsync(m => m.estado == "Ocupada");

                var ordenesAgrupadas = await _context.ordenes
                    .Where(o => o.codigo_orden != null)
                    .GroupBy(o => o.codigo_orden)
                    .Select(g => new OrdenResumenViewModel
                    {
                        Codigo = g.Key,
                        Cliente = g.First().nombre_cliente,
                        Fecha = g.First().fecha,
                        Total = g.Sum(x => x.total ?? 0)
                    })
                    .OrderByDescending(o => o.Fecha)
                    .Take(5)
                    .ToListAsync();

                ViewData["OrdenesRecientes"] = ordenesAgrupadas;

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

    public class OrdenResumenViewModel
    {
        public Guid? Codigo { get; set; }
        public string Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
    }
}
