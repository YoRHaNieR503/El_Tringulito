using Microsoft.AspNetCore.Mvc;
using El_Tringulito.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using El_Tringulito.Hubs;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace El_Tringulito.Controllers
{
    [Authorize(Roles = "admin,cocina")]
    public class CocinaController : Controller
    {
        private readonly ElTriangulitoDBContext _context;
        private readonly IHubContext<CocinaHub> _hubContext;

        public CocinaController(ElTriangulitoDBContext context, IHubContext<CocinaHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private IActionResult ValidarAccesoCocina()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("LoginCocina", "Auth");

            var rol = User.FindFirstValue(ClaimTypes.Role);
            if (rol != "cocina" && rol != "admin")
                return RedirectToAction("AccessDenied", "Auth");

            return null;
        }

        public async Task<IActionResult> Index()
        {
            var validacion = ValidarAccesoCocina();
            if (validacion != null) return validacion;

            var viewModel = new List<OrdenCocinaViewModel>();

            await ProcesarOrdenesMesas(viewModel);
            await ProcesarOrdenesParaLlevar(viewModel);

            var resultado = viewModel
                .OrderBy(v => v.EstadoGeneral == "Pendiente" ? 0 :
                              v.EstadoGeneral == "En Proceso" ? 1 : 2)
                .ThenBy(v => v.MesaNombre)
                .ToList();

            return View(resultado);
        }

        private async Task ProcesarOrdenesMesas(List<OrdenCocinaViewModel> viewModel)
        {
            var mesasConOrdenes = await _context.mesas
                .Where(m => _context.ordenes.Any(o => o.id_mesa == m.id_mesa &&
                    (o.estado == "Pendiente" || o.estado == "En Proceso" || o.estado == "Entregada")))
                .ToListAsync();

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
                bool tieneParaLlevar = false;
                bool tieneParaConsumir = false;

                foreach (var orden in ordenesBase)
                {
                    var item = await CrearItemViewModel(orden);
                    ordenes.Add(item);

                    if (item.ParaLlevar) tieneParaLlevar = true;
                    else tieneParaConsumir = true;
                }

                if (ordenes.Any())
                {
                    viewModel.Add(new OrdenCocinaViewModel
                    {
                        MesaId = mesa.id_mesa,
                        MesaNombre = mesa.nombre,
                        NombreCliente = ordenes.First().NombreCliente,
                        Ordenes = ordenes,
                        EstadoGeneral = CalcularEstadoGeneral(ordenes),
                        Total = ordenes.Sum(o => o.Precio),
                        EsParaLlevar = ordenes.Any(o => o.ParaLlevar),
                        TieneParaLlevar = tieneParaLlevar,
                        TieneParaConsumirEnSitio = tieneParaConsumir,
                        InfoMesa = ordenes.First().MesaNombre,
                        CodigoOrden = ordenesBase.FirstOrDefault(o => o.codigo_orden != null)?.codigo_orden
                    });
                }
            }
        }

        private async Task ProcesarOrdenesParaLlevar(List<OrdenCocinaViewModel> viewModel)
        {
            var ordenesParaLlevar = await _context.ordenes
                .Where(o => o.para_llevar &&
                           (o.estado == "Pendiente" || o.estado == "En Proceso" || o.estado == "Entregada") &&
                           o.codigo_orden != null)
                .GroupBy(o => o.codigo_orden)
                .ToListAsync();

            foreach (var grupo in ordenesParaLlevar)
            {
                // Si ya está incluida en una orden con mesa, omitir
                if (viewModel.Any(vm => vm.CodigoOrden == grupo.Key)) continue;

                var ordenes = new List<OrdenItemViewModel>();
                foreach (var orden in grupo)
                {
                    var item = await CrearItemViewModel(orden);
                    ordenes.Add(item);
                }

                if (ordenes.Any())
                {
                    string nombreCliente = ordenes.First().NombreCliente ?? "Cliente no especificado";
                    string mesaInfo = ordenes.First().MesaNombre ?? "Para Llevar";

                    viewModel.Add(new OrdenCocinaViewModel
                    {
                        MesaId = -1,
                        MesaNombre = $"Orden Para Llevar #{grupo.Key.ToString().Substring(0, 8)}",
                        NombreCliente = nombreCliente,
                        Ordenes = ordenes,
                        EstadoGeneral = CalcularEstadoGeneral(ordenes),
                        Total = ordenes.Sum(o => o.Precio),
                        EsParaLlevar = true,
                        TieneParaLlevar = true,
                        TieneParaConsumirEnSitio = false,
                        InfoMesa = mesaInfo,
                        CodigoOrden = grupo.Key
                    });
                }
            }
        }

        private async Task<OrdenItemViewModel> CrearItemViewModel(Ordenes orden)
        {
            string nombreProducto = await ObtenerNombreProducto(orden);
            string tipoProducto = ObtenerTipoProducto(orden);
            string mesaNombre = orden.id_mesa != null
                ? (await _context.mesas.FindAsync(orden.id_mesa))?.nombre
                : null;

            return new OrdenItemViewModel
            {
                IdOrden = orden.id_orden,
                NombreProducto = nombreProducto,
                TipoProducto = tipoProducto,
                Comentario = orden.comentario,
                Estado = orden.estado,
                Fecha = orden.fecha,
                Precio = orden.total ?? 0,
                NombreCliente = orden.nombre_cliente,
                ParaLlevar = orden.para_llevar,
                MesaNombre = mesaNombre,
                TipoOrden = orden.para_llevar ? "Para Llevar" : "Consumo en sitio"
            };
        }

        private string CalcularEstadoGeneral(List<OrdenItemViewModel> ordenes)
        {
            if (ordenes.Any(o => o.Estado == "Pendiente")) return "Pendiente";
            if (ordenes.Any(o => o.Estado == "En Proceso")) return "En Proceso";
            return "Entregada";
        }

        [HttpPost]
        public async Task<IActionResult> TomarOrden(int mesaId)
        {
            var validacion = ValidarAccesoCocina();
            if (validacion != null) return validacion;

            var query = mesaId == -1
                ? _context.ordenes.Where(o => o.para_llevar && o.estado == "Pendiente")
                : _context.ordenes.Where(o => o.id_mesa == mesaId && o.estado == "Pendiente");

            var ordenesPendientes = await query.ToListAsync();

            foreach (var orden in ordenesPendientes)
            {
                orden.estado = "En Proceso";
                orden.fecha = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrdenTomada", mesaId);


            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EntregarOrden(int mesaId)
        {
            var validacion = ValidarAccesoCocina();
            if (validacion != null) return validacion;

            var query = mesaId == -1
                ? _context.ordenes.Where(o => o.para_llevar && o.estado == "En Proceso")
                : _context.ordenes.Where(o => o.id_mesa == mesaId && o.estado == "En Proceso");

            var ordenesEnProceso = await query.ToListAsync();

            foreach (var orden in ordenesEnProceso)
            {
                orden.estado = "Entregada";
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrdenEntregada", mesaId);

            return RedirectToAction("Index");
        }

        private async Task<string> ObtenerNombreProducto(Ordenes orden)
        {
            if (orden.id_plato != null)
            {
                var plato = await _context.platos.FindAsync(orden.id_plato);
                return plato != null ? plato.nombre : "Plato no encontrado";
            }

            if (orden.id_promocion != null)
            {
                var promo = await _context.promociones.FindAsync(orden.id_promocion);
                if (promo == null)
                    return "Promoción no encontrada";

                string titulo = "Promoción";
                List<string> detalles = new();

                if (promo.id_plato != null)
                {
                    var plato = await _context.platos.FindAsync(promo.id_plato);
                    if (plato != null)
                        detalles.Add($"🍽️ <strong>Plato:</strong> {plato.nombre}");
                }

                if (promo.id_combo != null)
                {
                    var combo = await _context.combos.FindAsync(promo.id_combo);
                    if (combo != null)
                        detalles.Add($"🎁 <strong>Combo:</strong> {combo.nombre}");
                }

                if (promo.id_bebida != null)
                {
                    var bebida = await _context.bebidas.FindAsync(promo.id_bebida);
                    if (bebida != null)
                        detalles.Add($"🥤 <strong>Bebida:</strong> {bebida.nombre}");
                }

                if (detalles.Any())
                    return $"{titulo}<br><div style='margin-left:10px;'>{string.Join("<br>", detalles)}</div>";
                else
                    return "Promoción sin detalles";
            }

            if (orden.id_combo != null)
            {
                var combo = await _context.combos.FindAsync(orden.id_combo);
                return combo != null ? combo.nombre : "Combo no encontrado";
            }

            if (orden.id_bebida != null)
            {
                var bebida = await _context.bebidas.FindAsync(orden.id_bebida);
                return bebida != null ? bebida.nombre : "Bebida no encontrada";
            }

            return "Producto no identificado";
        }



        private string ObtenerTipoProducto(Ordenes orden)
        {
            if (orden.id_plato != null) return "Plato";
            if (orden.id_promocion != null) return "Promoción";
            if (orden.id_combo != null) return "Combo";
            if (orden.id_bebida != null) return "Bebida";
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
        public bool EsParaLlevar { get; set; }
        public string InfoMesa { get; set; }
        public bool TieneParaLlevar { get; set; }
        public bool TieneParaConsumirEnSitio { get; set; }
        public Guid? CodigoOrden { get; set; } // agregado para identificar duplicados
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
        public bool ParaLlevar { get; set; }
        public string MesaNombre { get; set; }
        public string TipoOrden { get; set; }
    }
}