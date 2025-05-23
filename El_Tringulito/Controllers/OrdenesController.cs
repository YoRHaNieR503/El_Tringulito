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

        public IActionResult CreateParaLlevar()
        {
            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParaLlevar(
            [Bind("id_orden,id_mesa,id_plato,id_promocion,id_bebida,id_combo,nombre_cliente,fecha,estado,comentario,total")]
            Ordenes orden)
        {
            if (orden.id_bebida == 0)
                orden.id_bebida = null;

            if (orden.id_bebida.HasValue)
            {
                var existe = await _context.bebidas.AnyAsync(b => b.id_bebida == orden.id_bebida.Value);
                if (!existe)
                {
                    ModelState.AddModelError("id_bebida", "La bebida seleccionada no existe.");
                }
            }

            if (ModelState.IsValid)
            {
                orden.id_mesa = null;
                _context.Add(orden);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre", orden.id_bebida);
            return View(orden);
        }

        public IActionResult Create()
        {
            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,id_bebida,nombre_cliente,fecha,estado,comentario,total")]
            Ordenes ordenes)
        {
            if (ordenes.id_mesa == 0)
                ordenes.id_mesa = null;

            if (ordenes.id_bebida == 0)
                ordenes.id_bebida = null;

            if (ordenes.id_bebida.HasValue)
            {
                var existe = await _context.bebidas.AnyAsync(b => b.id_bebida == ordenes.id_bebida.Value);
                if (!existe)
                {
                    ModelState.AddModelError("id_bebida", "La bebida seleccionada no existe.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(ordenes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre", ordenes.id_bebida);
            return View(ordenes);
        }

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
            if (orden == null) return NotFound();

            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre", orden.id_bebida);
            return View(orden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,id_bebida,nombre_cliente,fecha,estado,comentario,total")]
            Ordenes ordenes)
        {
            if (id != ordenes.id_orden) return NotFound();

            if (ordenes.id_bebida == 0)
                ordenes.id_bebida = null;

            if (ordenes.id_bebida.HasValue)
            {
                var existe = await _context.bebidas.AnyAsync(b => b.id_bebida == ordenes.id_bebida.Value);
                if (!existe)
                {
                    ModelState.AddModelError("id_bebida", "La bebida seleccionada no existe.");
                }
            }

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

            ViewBag.id_bebida = new SelectList(_context.bebidas, "id_bebida", "nombre", ordenes.id_bebida);
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
                    return BadRequest("Código de orden inválido");

                var ordenes = await _context.ordenes
                    .Where(o => o.codigo_orden == codigoOrden)
                    .OrderBy(o => o.fecha)
                    .ToListAsync();

                if (!ordenes.Any())
                    return NotFound("No se encontró la orden solicitada");

                var platoIds = ordenes.Where(o => o.id_plato.HasValue).Select(o => o.id_plato.Value).Distinct().ToList();
                var comboIds = ordenes.Where(o => o.id_combo.HasValue).Select(o => o.id_combo.Value).Distinct().ToList();
                var promocionIds = ordenes.Where(o => o.id_promocion.HasValue).Select(o => o.id_promocion.Value).Distinct().ToList();
                var bebidaIds = ordenes.Where(o => o.id_bebida.HasValue).Select(o => o.id_bebida.Value).Distinct().ToList();

                var platos = await _context.platos.Where(p => platoIds.Contains(p.id_plato)).ToDictionaryAsync(p => p.id_plato);
                var combos = await _context.combos.Where(c => comboIds.Contains(c.id_combo)).ToDictionaryAsync(c => c.id_combo);
                var bebidas = await _context.bebidas.Where(b => bebidaIds.Contains(b.id_bebida)).ToDictionaryAsync(b => b.id_bebida);
                var promociones = await _context.promociones.Where(p => promocionIds.Contains(p.id_promocion)).ToListAsync();

                var promocionesConProductos = new Dictionary<int, (Promociones, Platos, combos)>();
                foreach (var promo in promociones)
                {
                    var plato = promo.id_plato.HasValue ? await _context.platos.FindAsync(promo.id_plato.Value) : null;
                    var combo = promo.id_combo.HasValue ? await _context.combos.FindAsync(promo.id_combo.Value) : null;
                    promocionesConProductos[promo.id_promocion] = (promo, plato, combo);
                }

                var factura = new FacturaViewModel
                {
                    CodigoOrden = codigoOrden,
                    Cliente = ordenes.First().nombre_cliente ?? "Cliente no especificado",
                    Fecha = ordenes.First().fecha.ToString("dd/MM/yyyy HH:mm"),
                    EsParaLlevar = ordenes.First().id_mesa == null || ordenes.First().id_mesa == 0,
                    NumeroMesa = ordenes.First().id_mesa?.ToString() ?? "N/A",
                    Items = new List<FacturaItemViewModel>(),
                    Total = ordenes.Sum(o => o.total ?? 0)
                };

                foreach (var orden in ordenes)
                {
                    var item = new FacturaItemViewModel
                    {
                        Comentario = string.IsNullOrWhiteSpace(orden.comentario) ? "Ninguno" : orden.comentario,
                        Cantidad = 1
                    };

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
                    else if (orden.id_promocion.HasValue && promocionesConProductos.TryGetValue(orden.id_promocion.Value, out var promo))
                    {
                        item.Tipo = "Promoción";
                        item.Precio = orden.total ?? promo.Item1.precio ?? 0;
                        var nombres = new List<string>();
                        if (promo.Item2 != null) nombres.Add(promo.Item2.nombre);
                        if (promo.Item3 != null) nombres.Add(promo.Item3.nombre ?? "Combo");
                        item.Nombre = nombres.Any() ? $"Promoción ({string.Join(" + ", nombres)})" : "Promoción especial";
                    }
                    else if (orden.id_bebida.HasValue && bebidas.TryGetValue(orden.id_bebida.Value, out var bebida))
                    {
                        item.Tipo = "Bebida";
                        item.Nombre = bebida.nombre;
                        item.Precio = orden.total ?? bebida.precio;
                    }
                    else
                    {
                        item.Tipo = "Producto";
                        item.Nombre = "Producto no identificado";
                        item.Precio = orden.total ?? 0;
                    }

                    factura.Items.Add(item);
                }

                return View("VerFactura", factura);
            }
            catch
            {
                return StatusCode(500, "Ocurrió un error al procesar la solicitud. Por favor intente nuevamente.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBebidas()
        {
            var bebidas = await _context.bebidas.ToListAsync();
            var resultado = bebidas.Select(b => new
            {
                id = b.id_bebida,
                nombre = b.nombre,
                precio = b.precio
            });

            return Json(resultado);
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
        public decimal Subtotal => Total / 1.12m;
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
