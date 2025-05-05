using Microsoft.AspNetCore.Mvc;
using El_Tringulito.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using El_Tringulito.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace El_Tringulito.Controllers
{
    [Authorize(Roles = "admin,mesero")]
    public class MesasMeseroController : Controller
    {
        private readonly ElTriangulitoDBContext _context;
        private readonly IHubContext<CocinaHub> _hubContext;

        public MesasMeseroController(ElTriangulitoDBContext context, IHubContext<CocinaHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            var mesas = _context.mesas.ToList();
            return View(mesas);
        }

        public IActionResult Reservar(int id)
        {
            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id);
            if (mesa == null || mesa.estado != "Libre")
            {
                return NotFound();
            }
            return View(mesa);
        }

        public IActionResult VerOrden(int id)
        {
            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id);
            if (mesa == null)
            {
                return NotFound();
            }

            var ordenesActivas = _context.ordenes
                .Where(o => o.id_mesa == id && o.estado != "Finalizada")
                .ToList()
                .Select(o => new
                {
                    Orden = o,
                    NombreProducto = o.id_plato != null ? _context.platos.FirstOrDefault(p => p.id_plato == o.id_plato)?.nombre :
                                      o.id_promocion != null ? GetNombrePromocion(o.id_promocion.Value) :
                                      o.id_combo != null ? _context.combos.FirstOrDefault(c => c.id_combo == o.id_combo)?.nombre : "",
                    TipoProducto = o.id_plato != null ? "Plato" :
                                 o.id_promocion != null ? "Promoción" :
                                 o.id_combo != null ? "Combo" : ""
                })
                .ToList();

            var estadoGeneral = ordenesActivas.Any() ?
                (ordenesActivas.Any(o => o.Orden.estado == "En Proceso") ? "En Proceso" :
                 ordenesActivas.Any(o => o.Orden.estado == "Entregada") ? "Entregada" : "Pendiente") :
                "Sin Orden";

            var puedeFinalizar = ordenesActivas.Any() && ordenesActivas.All(o => o.Orden.estado == "Entregada");

            ViewBag.OrdenesActivas = ordenesActivas;
            ViewBag.TotalOrdenes = ordenesActivas.Sum(o => o.Orden.total);
            ViewBag.NombreCliente = ordenesActivas.FirstOrDefault()?.Orden.nombre_cliente ?? "";
            ViewBag.EstadoGeneral = estadoGeneral;
            ViewBag.PuedeFinalizar = puedeFinalizar;

            return View(mesa);
        }

        [HttpPost]
        public async Task<IActionResult> CrearOrden(int id_mesa, string nombre_cliente, decimal total, List<ProductoSeleccionado> productos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (productos == null || !productos.Any())
            {
                return BadRequest("No se seleccionaron productos.");
            }

            foreach (var producto in productos)
            {
                var precioProducto = await ObtenerPrecioProducto(producto);
                var nuevaOrden = new Ordenes
                {
                    id_mesa = id_mesa,
                    nombre_cliente = nombre_cliente,
                    comentario = producto.comentario,
                    fecha = DateTime.Now,
                    estado = "Pendiente",
                    total = precioProducto
                };

                if (producto.tipo == "platos")
                {
                    nuevaOrden.id_plato = producto.id;
                }
                else if (producto.tipo == "promociones")
                {
                    nuevaOrden.id_promocion = producto.id;
                }
                else if (producto.tipo == "combos")
                {
                    nuevaOrden.id_combo = producto.id;
                }

                _context.ordenes.Add(nuevaOrden);
            }

            await _context.SaveChangesAsync();

            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id_mesa);
            if (mesa != null)
            {
                mesa.estado = "Ocupada";
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients.All.SendAsync("NuevaOrdenCreada", id_mesa);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarOrden(int id_mesa, string nombre_cliente, decimal total, List<ProductoSeleccionado> productos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ordenEnProceso = _context.ordenes.Any(o => o.id_mesa == id_mesa && o.estado == "En Proceso");

            if (ordenEnProceso)
            {
                if (productos != null && productos.Any())
                {
                    foreach (var producto in productos)
                    {
                        var precioProducto = await ObtenerPrecioProducto(producto);
                        var nuevaOrden = new Ordenes
                        {
                            id_mesa = id_mesa,
                            nombre_cliente = nombre_cliente,
                            comentario = producto.comentario,
                            fecha = DateTime.Now,
                            estado = "Pendiente",
                            total = precioProducto
                        };

                        if (producto.tipo == "platos")
                        {
                            nuevaOrden.id_plato = producto.id;
                        }
                        else if (producto.tipo == "promociones")
                        {
                            nuevaOrden.id_promocion = producto.id;
                        }
                        else if (producto.tipo == "combos")
                        {
                            nuevaOrden.id_combo = producto.id;
                        }

                        _context.ordenes.Add(nuevaOrden);
                    }
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("NuevaOrdenAgregada", id_mesa);
                }
            }
            else
            {
                var ordenesExistentes = _context.ordenes
                    .Where(o => o.id_mesa == id_mesa && o.estado == "Pendiente")
                    .ToList();

                _context.ordenes.RemoveRange(ordenesExistentes);
                await _context.SaveChangesAsync();

                if (productos != null && productos.Any())
                {
                    foreach (var producto in productos)
                    {
                        var precioProducto = await ObtenerPrecioProducto(producto);
                        var nuevaOrden = new Ordenes
                        {
                            id_mesa = id_mesa,
                            nombre_cliente = nombre_cliente,
                            comentario = producto.comentario,
                            fecha = DateTime.Now,
                            estado = "Pendiente",
                            total = precioProducto
                        };

                        if (producto.tipo == "platos")
                        {
                            nuevaOrden.id_plato = producto.id;
                        }
                        else if (producto.tipo == "promociones")
                        {
                            nuevaOrden.id_promocion = producto.id;
                        }
                        else if (producto.tipo == "combos")
                        {
                            nuevaOrden.id_combo = producto.id;
                        }

                        _context.ordenes.Add(nuevaOrden);
                    }
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("NuevaOrdenCreada", id_mesa);
                }
            }

            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id_mesa);
            if (mesa != null)
            {
                mesa.estado = "Ocupada";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("VerOrden", new { id = id_mesa });
        }

        private async Task<decimal> ObtenerPrecioProducto(ProductoSeleccionado producto)
        {
            decimal precio = 0;
            if (producto.tipo == "platos")
            {
                precio = await _context.platos.Where(p => p.id_plato == producto.id).Select(p => p.precio).FirstOrDefaultAsync();
            }
            else if (producto.tipo == "promociones")
            {
                precio = await _context.promociones
     .Where(p => p.id_promocion == producto.id)
     .Select(p => p.precio)
     .FirstOrDefaultAsync() ?? 0;

            }
            else if (producto.tipo == "combos")
            {
                precio = await _context.combos.Where(c => c.id_combo == producto.id).Select(c => c.precio).FirstOrDefaultAsync();
            }
            return precio;
        }

        private string GetNombrePromocion(int idPromocion)
        {
            var promocion = _context.promociones.FirstOrDefault(p => p.id_promocion == idPromocion);
            if (promocion == null) return "Promoción no encontrada";

            if (promocion.id_plato != null)
            {
                return _context.platos.FirstOrDefault(p => p.id_plato == promocion.id_plato)?.nombre + " (Promoción)";
            }
            else if (promocion.id_combo != null)
            {
                return _context.combos.FirstOrDefault(c => c.id_combo == promocion.id_combo)?.nombre + " (Promoción)";
            }

            return "Promoción sin producto asociado";
        }

        public async Task<IActionResult> GetPlatos()
        {
            var platos = await _context.platos.ToListAsync();
            return Json(platos);
        }

        public async Task<IActionResult> GetPromociones()
        {
            var promociones = await _context.promociones
                .Where(p => p.fecha_inicio <= DateTime.Now && p.fecha_fin >= DateTime.Now)
                .ToListAsync();

            var promocionesConNombre = promociones.Select(p => new
            {
                p.id_promocion,
                p.precio,
                nombre = GetNombrePromocion(p.id_promocion),
                descripcion = "Promoción disponible"
            }).ToList();

            return Json(promocionesConNombre);
        }

        public async Task<IActionResult> GetCombos()
        {
            var combos = await _context.combos.ToListAsync();
            return Json(combos);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarOrden(int id_mesa)
        {
            var mesa = await _context.mesas.FindAsync(id_mesa);
            if (mesa == null)
            {
                return NotFound();
            }

            var ordenes = await _context.ordenes
                .Where(o => o.id_mesa == id_mesa && o.estado != "Finalizada")
                .ToListAsync();

            foreach (var orden in ordenes)
            {
                orden.estado = "Finalizada";
            }

            mesa.estado = "Libre";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Orden de la mesa {id_mesa} finalizada y mesa liberada";
            return RedirectToAction("Index");
        }

        public class ProductoSeleccionado
        {
            public int id { get; set; }
            public string tipo { get; set; }
            public string? comentario { get; set; }
            public decimal precio { get; set; }
        }
    }
}
