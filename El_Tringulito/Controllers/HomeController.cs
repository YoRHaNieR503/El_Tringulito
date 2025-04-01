using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using El_Tringulito.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElTriangulitoP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public HomeController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Obtener conteos de las entidades
            var combosCount = await _context.combos.CountAsync();
            var mesasCount = await _context.mesas.CountAsync();
            var ordenesCount = await _context.ordenes.CountAsync();
            var platosCount = await _context.platos.CountAsync();

            // Obtener las órdenes recientes (últimas 5 órdenes)
            var ordenesRecientes = await _context.ordenes
                .OrderByDescending(o => o.fecha)
                .Take(5)
                .ToListAsync();

            // Obtener el total de ventas (suma de los totales de las órdenes)
            var totalVentas = await _context.ordenes.SumAsync(o => o.total ?? 0);

            // Obtener el estado de las mesas (disponibles y ocupadas)
            var mesasDisponibles = await _context.mesas.CountAsync(m => m.estado == "Disponible");
            var mesasOcupadas = await _context.mesas.CountAsync(m => m.estado == "Ocupada");

            // Pasar los datos a la vista
            ViewData["CombosCount"] = combosCount;
            ViewData["MesasCount"] = mesasCount;
            ViewData["OrdenesCount"] = ordenesCount;
            ViewData["PlatosCount"] = platosCount;
            ViewData["OrdenesRecientes"] = ordenesRecientes;
            ViewData["TotalVentas"] = totalVentas;
            ViewData["MesasDisponibles"] = mesasDisponibles;
            ViewData["MesasOcupadas"] = mesasOcupadas;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}