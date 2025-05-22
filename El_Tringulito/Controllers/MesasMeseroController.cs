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
            if (mesa == null || mesa.estado != "Libre") return NotFound();

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

            var ordenesAgrupadas = ordenesActivas
        .GroupBy(o => o.Orden.codigo_orden.ToString()) // Convertir a string aquí
        .ToList();

            ViewBag.OrdenesAgrupadas = ordenesAgrupadas;

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
            ViewBag.CodigoOrden = id;


            var ordenFake = new Mesas { id_mesa = 0, nombre = "Para Llevar" };
            return View("VerOrden", ordenFake);
        }

        [HttpPost]
        public async Task<IActionResult> CrearOrden(int id_mesa, string nombre_cliente, decimal total, List<ProductoSeleccionado> productos)
        {
            if (!ModelState.IsValid || productos == null || !productos.Any())
                return BadRequest("Datos inválidos o productos vacíos.");

            Guid codigoOrden = Guid.NewGuid(); // Se genera siempre

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
                    para_llevar = producto.paraLlevar,
                    codigo_orden = codigoOrden
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
            ActualizarEstadosPromociones();

            var promociones = await _context.promociones
                .Where(p => p.estado == "activa")
                .ToListAsync();

            var promos = promociones.Select(p =>
            {
                string nombre = GetNombrePromocionTitulo(p);
                string descripcion = GetDescripcionPromocion(p);

                return new
                {
                    p.id_promocion,
                    p.precio,
                    nombre,
                    descripcion
                };
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
        private string GetNombrePromocionTitulo(Promociones promo)
        {
            bool tienePlato = promo.id_plato.HasValue;
            bool tieneCombo = promo.id_combo.HasValue;

            if (tienePlato && tieneCombo)
                return "Promoción: Plato + Combo";
            else if (tienePlato)
                return "Promoción: Plato";
            else if (tieneCombo)
                return "Promoción: Combo";

            return "Promoción";
        }


        private string GetDescripcionPromocion(Promociones promo)
        {
            List<string> partes = new List<string>();

            if (promo.id_plato.HasValue)
            {
                var plato = _context.platos.FirstOrDefault(p => p.id_plato == promo.id_plato);
                if (plato != null)
                    partes.Add($"<li><strong>Plato:</strong> {plato.nombre}</li>");
            }

            if (promo.id_combo.HasValue)
            {
                var combo = _context.combos.FirstOrDefault(c => c.id_combo == promo.id_combo);
                if (combo != null)
                    partes.Add($"<li><strong>Combo:</strong> {combo.nombre}</li>");
            }

            return partes.Any() ? $"<ul class='mb-0 ps-3'>{string.Join("", partes)}</ul>" : "Promoción sin detalles.";
        }



        [HttpPost]
        public async Task<IActionResult> ActualizarOrden(int id_mesa, string nombre_cliente, decimal total,
     List<ProductoSeleccionado> productos, Guid? codigoOrden)
        {
            if (!ModelState.IsValid || productos == null || !productos.Any())
                return BadRequest("Datos inválidos o productos vacíos.");

            bool esParaLlevar = id_mesa == 0;
            Guid codigoExistente;

            // Determinar el código de orden existente
            if (codigoOrden.HasValue && codigoOrden != Guid.Empty)
            {
                // Usar el código proporcionado
                codigoExistente = codigoOrden.Value;
            }
            else
            {
                // Buscar el código de orden existente para esta mesa o para llevar
                var ordenExistente = _context.ordenes
                    .Where(o => esParaLlevar
                        ? o.para_llevar && o.nombre_cliente == nombre_cliente && o.estado != "Finalizada"
                        : o.id_mesa == id_mesa && o.estado != "Finalizada")
                    .OrderByDescending(o => o.fecha)
                    .FirstOrDefault();

                codigoExistente = ordenExistente?.codigo_orden ?? Guid.NewGuid();
            }

            // Crear nuevas órdenes con el mismo código
            foreach (var producto in productos)
            {
                var precio = await ObtenerPrecioProducto(producto);
                var nuevaOrden = new Ordenes
                {
                    id_mesa = esParaLlevar ? null : id_mesa,
                    nombre_cliente = nombre_cliente,
                    comentario = producto.comentario,
                    fecha = DateTime.Now,
                    estado = "Pendiente",
                    total = precio,
                    para_llevar = esParaLlevar || producto.paraLlevar,
                    codigo_orden = codigoExistente
                };

                if (producto.tipo == "platos") nuevaOrden.id_plato = producto.id;
                else if (producto.tipo == "promociones") nuevaOrden.id_promocion = producto.id;
                else if (producto.tipo == "combos") nuevaOrden.id_combo = producto.id;

                _context.ordenes.Add(nuevaOrden);
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("NuevaOrdenAgregada", id_mesa);

            // Actualizar estado de la mesa si no es para llevar
            if (!esParaLlevar)
            {
                var mesa = await _context.mesas.FindAsync(id_mesa);
                if (mesa != null && mesa.estado != "Ocupada")
                {
                    mesa.estado = "Ocupada";
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("VerOrden", new { id = id_mesa });
            }

            return RedirectToAction("VerOrdenParaLlevar", new { id = codigoExistente });
        }




        [HttpPost]
        public async Task<IActionResult> CancelarOrden(int id_mesa)
        {
            var ordenes = _context.ordenes
                .Where(o => o.id_mesa == id_mesa && o.estado == "Pendiente")
                .ToList();

            if (!ordenes.Any())
            {
                TempData["ErrorMessage"] = "No se puede cancelar la orden. No hay productos en estado 'Pendiente'.";
                return RedirectToAction("VerOrden", new { id = id_mesa });
            }

            _context.ordenes.RemoveRange(ordenes);
            await _context.SaveChangesAsync();

            var mesa = await _context.mesas.FindAsync(id_mesa);
            if (mesa != null)
            {
                mesa.estado = "Libre";
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Orden cancelada exitosamente.";
            return RedirectToAction("VerOrden", new { id = id_mesa });
        }

        [HttpPost]
        public async Task<IActionResult> CancelarOrdenParaLlevar(Guid id)
        {
            var ordenes = _context.ordenes
                .Where(o => o.codigo_orden == id && o.estado == "Pendiente" && o.para_llevar)
                .ToList();

            if (!ordenes.Any())
            {
                TempData["ErrorMessage"] = "No se puede cancelar la orden. No hay productos pendientes o ya está en proceso.";
                return RedirectToAction("VerOrdenParaLlevar", new { id = id });
            }

            _context.ordenes.RemoveRange(ordenes);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Orden para llevar cancelada exitosamente.";
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> FinalizarOrden(int id_mesa)
        {
            var mesa = await _context.mesas.FirstOrDefaultAsync(m => m.id_mesa == id_mesa);
            if (mesa == null)
            {
                TempData["ErrorMessage"] = "Mesa no encontrada.";
                return RedirectToAction("Index");
            }

            // Obtener todas las órdenes no finalizadas de esa mesa
            var ordenes = await _context.ordenes
                .Where(o => o.id_mesa == id_mesa && o.estado != "Finalizada")
                .ToListAsync();

            if (!ordenes.Any())
            {
                TempData["ErrorMessage"] = "No hay órdenes activas para finalizar.";
                return RedirectToAction("VerOrden", new { id = id_mesa });
            }

            // Validar que todas estén en estado "Entregada"
            if (ordenes.Any(o => o.estado != "Entregada"))
            {
                TempData["ErrorMessage"] = "Solo se puede finalizar la orden si todos los productos han sido entregados.";
                return RedirectToAction("VerOrden", new { id = id_mesa });
            }

            // Cambiar estado a "Finalizada"
            foreach (var orden in ordenes)
            {
                orden.estado = "Finalizada";
            }

            // Liberar la mesa
            mesa.estado = "Libre";

            await _context.SaveChangesAsync();


            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> FinalizarOrdenParaLlevar(Guid codigo_orden)
        {
            // Obtener todas las órdenes para llevar con ese código
            var ordenes = await _context.ordenes
                .Where(o => o.codigo_orden == codigo_orden && o.para_llevar && o.estado != "Finalizada")
                .ToListAsync();

            if (!ordenes.Any())
            {
                TempData["ErrorMessage"] = "No hay órdenes activas para finalizar.";
                return RedirectToAction("Index");
            }

            // Verificar que todas estén entregadas
            if (ordenes.Any(o => o.estado != "Entregada"))
            {
                TempData["ErrorMessage"] = "Solo se puede finalizar si todos los productos han sido entregados.";
                return RedirectToAction("VerOrdenParaLlevar", new { id = codigo_orden });
            }

            // Cambiar estado a Finalizada
            foreach (var orden in ordenes)
            {
                orden.estado = "Finalizada";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }












        public IActionResult ParaLlevar()
        {
            var validacion = ValidarAccesoMesero();
            if (validacion != null) return validacion;

            return View(); // Vista: Views/MesasMesero/ParaLlevar.cshtml
        }





        private void ActualizarEstadosPromociones()
        {
            var hoy = DateTime.Now;
            var promociones = _context.promociones.ToList();

            foreach (var promo in promociones)
            {
                if (promo.fecha_fin.HasValue && promo.fecha_fin.Value < hoy)
                {
                    promo.estado = "vencida";
                }
                else
                {
                    promo.estado = "activa";
                }
            }

            _context.SaveChanges();
        }







        public class ProductoSeleccionado
        {
            public int id { get; set; }
            public string tipo { get; set; }
            public string? comentario { get; set; }
            public decimal precio { get; set; }
            public bool paraLlevar { get; set; }
        }
    }
}
