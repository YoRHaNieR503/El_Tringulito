using Microsoft.AspNetCore.Mvc;
using El_Tringulito.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using El_Tringulito.Hubs;
using System.Collections.Generic;
using System;

namespace El_Tringulito.Controllers
{
    public class CocinaController : Controller
    {
        private readonly ElTriangulitoDBContext _context;
        private readonly IHubContext<CocinaHub> _hubContext;

        public CocinaController(ElTriangulitoDBContext context, IHubContext<CocinaHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var mesasConOrdenes = await _context.mesas
                .Where(m => _context.ordenes.Any(o => o.id_mesa == m.id_mesa &&
                    (o.estado == "Pendiente" || o.estado == "En Proceso" || o.estado == "Entregada")))
                .ToListAsync();

            var viewModel = new List<OrdenCocinaViewModel>();

            foreach (var mesa in mesasConOrdenes)
            {
                var ordenesBase = await _context.ordenes
                    .Where(o => o.id_mesa == mesa.id_mesa &&
                        (o.estado == "Pendiente" || o.estado == "En Proceso" || o.estado == "Entregada"))
                    .OrderBy(o => o.estado == "Pendiente" ? 0 :
                        o.estado == "En Proceso" ? 1 : 2)
                    .ThenBy(o => o.fecha)
                    .ToListAsync();

                var ordenes = new List<OrdenItemViewModel>();

                foreach (var orden in ordenesBase)
                {
                    string nombreProducto = await ObtenerNombreProducto(orden);
                    string tipoProducto = ObtenerTipoProducto(orden);

                    ordenes.Add(new OrdenItemViewModel
                    {
                        IdOrden = orden.id_orden,
                        NombreProducto = nombreProducto,
                        TipoProducto = tipoProducto,
                        Comentario = orden.comentario,
                        Estado = orden.estado,
                        Fecha = orden.fecha,
                        Precio = orden.total ?? 0,
                        NombreCliente = orden.nombre_cliente
                    });
                }

                if (ordenes.Any())
                {
                    var estadoGeneral = ordenes.Any(o => o.Estado == "Pendiente") ? "Pendiente" :
                                      ordenes.Any(o => o.Estado == "En Proceso") ? "En Proceso" : "Entregada";

                    viewModel.Add(new OrdenCocinaViewModel
                    {
                        MesaId = mesa.id_mesa,
                        MesaNombre = mesa.nombre,
                        NombreCliente = ordenes.First().NombreCliente,
                        Ordenes = ordenes,
                        EstadoGeneral = estadoGeneral,
                        Total = ordenes.Sum(o => o.Precio)
                    });
                }
            }

            var resultado = viewModel
                .OrderBy(v => v.EstadoGeneral == "Pendiente" ? 0 :
                              v.EstadoGeneral == "En Proceso" ? 1 : 2)
                .ThenBy(v => v.MesaNombre)
                .ToList();

            return View(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> TomarOrden(int mesaId)
        {
            var ordenesPendientes = await _context.ordenes
                .Where(o => o.id_mesa == mesaId && o.estado == "Pendiente")
                .ToListAsync();

            foreach (var orden in ordenesPendientes)
            {
                orden.estado = "En Proceso";
                orden.fecha = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrdenTomada", mesaId);

            TempData["SuccessMessage"] = $"Orden de la mesa {mesaId} tomada con éxito";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EntregarOrden(int mesaId)
        {
            var ordenesEnProceso = await _context.ordenes
                .Where(o => o.id_mesa == mesaId && o.estado == "En Proceso")
                .ToListAsync();

            foreach (var orden in ordenesEnProceso)
            {
                orden.estado = "Entregada";
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrdenEntregada", mesaId);

            TempData["SuccessMessage"] = $"Orden de la mesa {mesaId} marcada como entregada";
            return RedirectToAction("Index");
        }

        private async Task<string> ObtenerNombreProducto(Ordenes orden)
        {
            if (orden.id_plato != null)
            {
                var plato = await _context.platos.FindAsync(orden.id_plato);
                return plato?.nombre ?? "Plato no encontrado";
            }
            else if (orden.id_promocion != null)
            {
                var promocion = await _context.promociones.FindAsync(orden.id_promocion);
                if (promocion != null)
                {
                    if (promocion.id_plato != null)
                    {
                        var plato = await _context.platos.FindAsync(promocion.id_plato);
                        return plato?.nombre + " (Promoción)" ?? "Promoción no válida";
                    }
                    else if (promocion.id_combo != null)
                    {
                        var combo = await _context.combos.FindAsync(promocion.id_combo);
                        return combo?.nombre + " (Promoción)" ?? "Promoción no válida";
                    }
                }
                return "Promoción sin producto asociado";
            }
            else if (orden.id_combo != null)
            {
                var combo = await _context.combos.FindAsync(orden.id_combo);
                return combo?.nombre ?? "Combo no encontrado";
            }
            return "Producto no identificado";
        }

        private string ObtenerTipoProducto(Ordenes orden)
        {
            if (orden.id_plato != null) return "Plato";
            if (orden.id_promocion != null) return "Promoción";
            if (orden.id_combo != null) return "Combo";
            return "Desconocido";
        }
    }

    public class OrdenCocinaViewModel
    {
        public int MesaId { get; set; }
        public string MesaNombre { get; set; }
        public string NombreCliente { get; set; }
        public List<OrdenItemViewModel> Ordenes { get; set; }
        public string EstadoGeneral { get; set; }
        public decimal Total { get; set; }
    }

    public class OrdenItemViewModel
    {
        public int IdOrden { get; set; }
        public string NombreProducto { get; set; }
        public string TipoProducto { get; set; }
        public string Comentario { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Precio { get; set; }
        public string NombreCliente { get; set; }
    }
}
