using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using El_Tringulito.Models;

namespace El_Tringulito.Controllers
{
    public class OrdenesController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public OrdenesController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        // GET: Ordenes
        public async Task<IActionResult> Index()
        {
            var ordenes = await _context.ordenes.ToListAsync();
            ViewBag.OrdenesParaLlevar = ordenes.Where(o => o.id_mesa == null || o.id_mesa == 0).ToList();
            ViewBag.OrdenesEnLugar = ordenes.Where(o => o.id_mesa != null && o.id_mesa > 0).ToList();
            return View(ordenes);
        }

        // GET: Ordenes/CreateParaLlevar
        public IActionResult CreateParaLlevar()
        {
            return View();
        }

        // POST: Ordenes/CreateParaLlevar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParaLlevar(
            [Bind("id_orden,id_plato,id_promocion,id_combo,nombre_cliente,fecha,estado,comentario,total")] Ordenes orden)
        {
            if (ModelState.IsValid)
            {
                orden.id_mesa = null; // Explícitamente establecido como null
                _context.Add(orden);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(orden);
        }

        // GET: Ordenes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ordenes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,nombre_cliente,fecha,estado,comentario,total")] Ordenes ordenes)
        {
            if (ModelState.IsValid)
            {
                // Validación adicional para mesa
                if (ordenes.id_mesa == 0)
                {
                    ordenes.id_mesa = null;
                }
                _context.Add(ordenes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ordenes);
        }

        // Los demás métodos (Details, Edit, Delete) permanecen igual...
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var orden = await _context.ordenes.FirstOrDefaultAsync(m => m.id_orden == id);
            return orden == null ? NotFound() : View(orden);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var orden = await _context.ordenes.FindAsync(id);
            return orden == null ? NotFound() : View(orden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,nombre_cliente,fecha,estado,comentario,total")] Ordenes ordenes)
        {
            if (id != ordenes.id_orden) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ordenes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrdenesExists(ordenes.id_orden)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ordenes);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var orden = await _context.ordenes.FirstOrDefaultAsync(m => m.id_orden == id);
            return orden == null ? NotFound() : View(orden);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orden = await _context.ordenes.FindAsync(id);
            if (orden != null) _context.ordenes.Remove(orden);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }




        [Route("Ordenes/VerFactura/{codigoOrden}")]
        [HttpGet]
        public async Task<IActionResult> VerFactura(Guid codigoOrden)

        {
            try
            {
                if (codigoOrden == Guid.Empty)
                {
                    return BadRequest("Código de orden inválido");
                }

                // Obtener todas las órdenes con el mismo código_orden
                var ordenes = await _context.ordenes
                    .Where(o => o.codigo_orden == codigoOrden)
                    .OrderBy(o => o.fecha)
                    .ToListAsync();

                if (ordenes == null || !ordenes.Any())
                {
                    return NotFound("No se encontró la orden solicitada");
                }

                // Preparar listas de IDs para buscar en las tablas relacionadas
                var platoIds = ordenes
                    .Where(o => o.id_plato.HasValue)
                    .Select(o => o.id_plato.Value)
                    .Distinct()
                    .ToList();

                var comboIds = ordenes
                    .Where(o => o.id_combo.HasValue)
                    .Select(o => o.id_combo.Value)
                    .Distinct()
                    .ToList();

                var promocionIds = ordenes
                    .Where(o => o.id_promocion.HasValue)
                    .Select(o => o.id_promocion.Value)
                    .Distinct()
                    .ToList();

                // Obtener los productos de las tablas relacionadas
                var platos = await _context.platos
                    .Where(p => platoIds.Contains(p.id_plato))
                    .ToDictionaryAsync(p => p.id_plato);

                var combos = await _context.combos
                    .Where(c => comboIds.Contains(c.id_combo))
                    .ToDictionaryAsync(c => c.id_combo);

                // Obtener promociones con sus relaciones
                var promociones = await _context.promociones
                    .Where(p => promocionIds.Contains(p.id_promocion))
                    .ToListAsync();

                // Crear diccionario para las promociones con sus productos relacionados
                var promocionesConProductos = new Dictionary<int, (Promociones Promocion, Platos Plato, combos Combo)>();

                foreach (var promocion in promociones)
                {
                    Platos plato = null;
                    combos combo = null;

                    if (promocion.id_plato.HasValue)
                    {
                        plato = await _context.platos.FindAsync(promocion.id_plato.Value);
                    }

                    if (promocion.id_combo.HasValue)
                    {
                        combo = await _context.combos.FindAsync(promocion.id_combo.Value);
                    }

                    promocionesConProductos.Add(promocion.id_promocion, (promocion, plato, combo));
                }

                // Crear el modelo de la factura
                var primeraOrden = ordenes.First();
                var factura = new FacturaViewModel
                {
                    CodigoOrden = codigoOrden,
                    Cliente = primeraOrden.nombre_cliente ?? "Cliente no especificado",
                    Fecha = primeraOrden.fecha.ToString("dd/MM/yyyy HH:mm"),
                    EsParaLlevar = primeraOrden.id_mesa == null || primeraOrden.id_mesa == 0,
                    NumeroMesa = primeraOrden.id_mesa?.ToString() ?? "N/A",
                    Items = new List<FacturaItemViewModel>(),
                    Total = ordenes.Sum(o => o.total ?? 0)
                };

                // Procesar cada orden para construir los items de la factura
                foreach (var orden in ordenes)
                {
                    var item = new FacturaItemViewModel();

                    if (orden.id_plato.HasValue && platos.TryGetValue(orden.id_plato.Value, out var plato))
                    {
                        item.Tipo = "Plato";
                        item.Nombre = plato.nombre;
                        item.Precio = orden.total ?? plato.precio;
                    }
                    else if (orden.id_combo.HasValue && combos.TryGetValue(orden.id_combo.Value, out var combo))
                    {
                        item.Tipo = "Combo";
                        item.Nombre = combo.nombre ?? "Combo sin nombre";
                        item.Precio = orden.total ?? combo.precio;
                    }
                    else if (orden.id_promocion.HasValue &&
                             promocionesConProductos.TryGetValue(orden.id_promocion.Value, out var promocionInfo))
                    {
                        item.Tipo = "Promoción";
                        item.Precio = orden.total ?? promocionInfo.Promocion.precio ?? 0;

                        // Construir nombre descriptivo para la promoción
                        var nombres = new List<string>();
                        if (promocionInfo.Plato != null)
                        {
                            nombres.Add(promocionInfo.Plato.nombre);
                        }
                        if (promocionInfo.Combo != null)
                        {
                            nombres.Add(promocionInfo.Combo.nombre ?? "Combo");
                        }

                        item.Nombre = nombres.Any()
                            ? $"Promoción ({string.Join(" + ", nombres)})"
                            : "Promoción especial";
                    }
                    else
                    {
                        item.Tipo = "Producto";
                        item.Nombre = "Producto no identificado";
                        item.Precio = orden.total ?? 0;
                    }

                    item.Comentario = string.IsNullOrEmpty(orden.comentario) ? "Ninguno" : orden.comentario;
                    item.Cantidad = 1;

                    factura.Items.Add(item);
                }

                return View("VerFactura", factura);
            }
            catch (Exception ex)
            {
                // Loggear el error
                return StatusCode(500, "Ocurrió un error al procesar la solicitud. Por favor intente nuevamente.");
            }
        }





        private bool OrdenesExists(int id)
        {
            return _context.ordenes.Any(e => e.id_orden == id);
        }
    }

    public class FacturaViewModel
    {
        public Guid CodigoOrden { get; set; }
        public string Cliente { get; set; }
        public string Fecha { get; set; }
        public bool EsParaLlevar { get; set; }
        public string NumeroMesa { get; set; }
        public List<FacturaItemViewModel> Items { get; set; }
        public decimal Total { get; set; }
        public decimal Subtotal => Total / 1.12m; // Asumiendo 12% de IVA
        public decimal Iva => Total - Subtotal;
    }

    public class FacturaItemViewModel
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Comentario { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total => Cantidad * Precio;
    }
}