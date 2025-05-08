using Microsoft.AspNetCore.Mvc;
using El_Tringulito.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using El_Tringulito.Hubs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        private IActionResult ValidarAccesoMesero()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("LoginMesero", "Auth");

            var rol = User.FindFirstValue(ClaimTypes.Role);
            if (rol != "mesero" && rol != "admin")
                return RedirectToAction("AccessDenied", "Auth");

            return null;
        }

        public IActionResult Index()
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            ViewBag.OrdenesParaLlevar = _context.ordenes
                .Where(o => o.id_mesa == null && o.estado != "Finalizada" && o.codigo_orden != null)
                .OrderByDescending(o => o.fecha)
                .ToList();

            var mesas = _context.mesas.ToList();
            return View(mesas);
        }

        public IActionResult Reservar(int id)
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id);
            if (mesa == null || mesa.estado != "Libre")
            {
                return NotFound();
            }
            return View(mesa);
        }

        public IActionResult VerOrden(int id)
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id);
            if (mesa == null) return NotFound();

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

        public IActionResult VerOrdenParaLlevar(Guid id)
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            var ordenes = _context.ordenes
                .Where(o => o.codigo_orden == id && o.para_llevar && o.estado != "Finalizada")
                .ToList();

            if (!ordenes.Any()) return NotFound();

            var ordenesActivas = ordenes.Select(o => new
            {
                Orden = o,
                NombreProducto = o.id_plato != null ? _context.platos.FirstOrDefault(p => p.id_plato == o.id_plato)?.nombre :
                                  o.id_promocion != null ? GetNombrePromocion(o.id_promocion.Value) :
                                  o.id_combo != null ? _context.combos.FirstOrDefault(c => c.id_combo == o.id_combo)?.nombre : "",
                TipoProducto = o.id_plato != null ? "Plato" :
                             o.id_promocion != null ? "Promoción" :
                             o.id_combo != null ? "Combo" : ""
            }).ToList();

            var estadoGeneral = ordenesActivas.Any() ?
                (ordenesActivas.Any(o => o.Orden.estado == "En Proceso") ? "En Proceso" :
                 ordenesActivas.Any(o => o.Orden.estado == "Entregada") ? "Entregada" : "Pendiente") :
                "Sin Orden";

            var puedeFinalizar = ordenesActivas.All(o => o.Orden.estado == "Entregada");

            ViewBag.OrdenesActivas = ordenesActivas;
            ViewBag.TotalOrdenes = ordenesActivas.Sum(o => o.Orden.total);
            ViewBag.NombreCliente = ordenesActivas.FirstOrDefault()?.Orden.nombre_cliente ?? "";
            ViewBag.EstadoGeneral = estadoGeneral;
            ViewBag.PuedeFinalizar = puedeFinalizar;
            ViewBag.EsParaLlevar = true;

            var ordenFake = new Mesas { id_mesa = 0, nombre = "Para Llevar" };
            return View("VerOrden", ordenFake);
        }

        [HttpPost]
        public async Task<IActionResult> CrearOrden(int id_mesa, string nombre_cliente, decimal total, List<ProductoSeleccionado> productos)
        {
            if (!ModelState.IsValid || productos == null || !productos.Any())
                return BadRequest("Datos inválidos o productos vacíos.");

            foreach (var producto in productos)
            {
                var precio = await ObtenerPrecioProducto(producto);
                var orden = new Ordenes
                {
                    id_mesa = id_mesa,
                    nombre_cliente = nombre_cliente,
                    comentario = producto.comentario,
                    fecha = DateTime.Now,
                    estado = "Pendiente",
                    para_llevar = producto.paraLlevar,
                    total = precio
                };

                if (producto.tipo == "platos") orden.id_plato = producto.id;
                else if (producto.tipo == "promociones") orden.id_promocion = producto.id;
                else if (producto.tipo == "combos") orden.id_combo = producto.id;

                _context.ordenes.Add(orden);
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
            if (!ModelState.IsValid) return BadRequest();

            var enProceso = _context.ordenes.Any(o => o.id_mesa == id_mesa && o.estado == "En Proceso");

            if (enProceso)
            {
                if (productos != null && productos.Any())
                {
                    foreach (var producto in productos)
                    {
                        var precio = await ObtenerPrecioProducto(producto);
                        var orden = new Ordenes
                        {
                            id_mesa = id_mesa,
                            nombre_cliente = nombre_cliente,
                            comentario = producto.comentario,
                            fecha = DateTime.Now,
                            estado = "Pendiente",
                            total = precio,
                            para_llevar = producto.paraLlevar // Asegurar que se guarde el flag para llevar
                        };

                        if (producto.tipo == "platos") orden.id_plato = producto.id;
                        else if (producto.tipo == "promociones") orden.id_promocion = producto.id;
                        else if (producto.tipo == "combos") orden.id_combo = producto.id;

                        _context.ordenes.Add(orden);
                    }
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("NuevaOrdenAgregada", id_mesa);
                }
            }
            else
            {
                var anteriores = _context.ordenes
                    .Where(o => o.id_mesa == id_mesa && o.estado == "Pendiente").ToList();

                _context.ordenes.RemoveRange(anteriores);
                await _context.SaveChangesAsync();

                if (productos != null && productos.Any())   
                {
                    foreach (var producto in productos)
                    {
                        var precio = await ObtenerPrecioProducto(producto);
                        var orden = new Ordenes
                        {
                            id_mesa = id_mesa,
                            nombre_cliente = nombre_cliente,
                            comentario = producto.comentario,
                            fecha = DateTime.Now,
                            estado = "Pendiente",
                            total = precio,
                            para_llevar = producto.paraLlevar // Asegurar que se guarde el flag para llevar
                        };

                        if (producto.tipo == "platos") orden.id_plato = producto.id;
                        else if (producto.tipo == "promociones") orden.id_promocion = producto.id;
                        else if (producto.tipo == "combos") orden.id_combo = producto.id;

                        _context.ordenes.Add(orden);
                    }
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("NuevaOrdenCreada", id_mesa);
                }
            }

            var mesaUpd = _context.mesas.FirstOrDefault(m => m.id_mesa == id_mesa);
            if (mesaUpd != null)
            {
                mesaUpd.estado = "Ocupada";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("VerOrden", new { id = id_mesa });
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarOrden(int id_mesa)
        {
            var mesa = await _context.mesas.FindAsync(id_mesa);
            if (mesa == null) return NotFound();

            var ordenes = await _context.ordenes
                .Where(o => o.id_mesa == id_mesa && o.estado != "Finalizada").ToListAsync();

            foreach (var orden in ordenes)
            {
                orden.estado = "Finalizada";
            }

            mesa.estado = "Libre";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Orden de la mesa {id_mesa} finalizada y mesa liberada";
            return RedirectToAction("Index");
        }

        public IActionResult ParaLlevar()
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> AgregarProductoLlevar(int idMesa, int idProducto, string tipoProducto, string comentario)
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            // Obtener el precio del producto
            var precio = await ObtenerPrecioProducto(new ProductoSeleccionado { id = idProducto, tipo = tipoProducto });

            // Crear la orden para llevar asociada a la mesa
            var orden = new Ordenes
            {
                id_mesa = idMesa,
                nombre_cliente = _context.ordenes.FirstOrDefault(o => o.id_mesa == idMesa)?.nombre_cliente ?? "",
                comentario = comentario,
                fecha = DateTime.Now,
                estado = "Pendiente",
                total = precio,
                para_llevar = true
            };

            if (tipoProducto == "platos") orden.id_plato = idProducto;
            else if (tipoProducto == "promociones") orden.id_promocion = idProducto;
            else if (tipoProducto == "combos") orden.id_combo = idProducto;

            _context.ordenes.Add(orden);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("NuevaOrdenAgregada", idMesa);

            return RedirectToAction("VerOrden", new { id = idMesa });
        }



        [HttpPost]
        public async Task<IActionResult> CrearOrdenParaLlevar(string nombre_cliente, decimal total, List<ProductoSeleccionado> productos)
        {
            if (productos == null || !productos.Any())
                return BadRequest("Debe agregar al menos un producto.");

            Guid codigo = Guid.NewGuid();

            foreach (var producto in productos)
            {
                var precio = await ObtenerPrecioProducto(producto);
                var orden = new Ordenes
                {
                    id_mesa = null,
                    nombre_cliente = nombre_cliente,
                    comentario = producto.comentario,
                    fecha = DateTime.Now,
                    estado = "Pendiente",
                    total = precio,
                    para_llevar = true,
                    codigo_orden = codigo
                };

                if (producto.tipo == "platos") orden.id_plato = producto.id;
                else if (producto.tipo == "promociones") orden.id_promocion = producto.id;
                else if (producto.tipo == "combos") orden.id_combo = producto.id;

                _context.ordenes.Add(orden);
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NuevaOrdenCreada", -1);

            TempData["SuccessMessage"] = "Orden para llevar creada exitosamente";
            return RedirectToAction("Index");
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

            var promos = promociones.Select(p => new
            {
                p.id_promocion,
                p.precio,
                nombre = GetNombrePromocion(p.id_promocion),
                descripcion = "Promoción disponible"
            }).ToList();

            return Json(promos);
        }

        public async Task<IActionResult> GetCombos()
        {
            var combos = await _context.combos.ToListAsync();
            return Json(combos);
        }

        private async Task<decimal> ObtenerPrecioProducto(ProductoSeleccionado producto)
        {
            if (producto.tipo == "platos")
                return await _context.platos.Where(p => p.id_plato == producto.id).Select(p => p.precio).FirstOrDefaultAsync();
            else if (producto.tipo == "promociones")
                return await _context.promociones.Where(p => p.id_promocion == producto.id).Select(p => p.precio).FirstOrDefaultAsync() ?? 0;
            else if (producto.tipo == "combos")
                return await _context.combos.Where(c => c.id_combo == producto.id).Select(c => c.precio).FirstOrDefaultAsync();

            return 0;
        }

        private string GetNombrePromocion(int idPromocion)
        {
            var promo = _context.promociones.FirstOrDefault(p => p.id_promocion == idPromocion);
            if (promo == null) return "Promoción no encontrada";

            if (promo.id_plato != null)
                return _context.platos.FirstOrDefault(p => p.id_plato == promo.id_plato)?.nombre + " (Promoción)";
            else if (promo.id_combo != null)
                return _context.combos.FirstOrDefault(c => c.id_combo == promo.id_combo)?.nombre + " (Promoción)";

            return "Promoción sin producto asociado";
        }

        public class ProductoSeleccionado
        {
            public int id { get; set; }
            public string tipo { get; set; }
            public string? comentario { get; set; }
            public decimal precio { get; set; }
            public bool paraLlevar { get; set; } // Nueva propiedad
        }
    }
}
