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
    public class PromocionesController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public PromocionesController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;

            var promociones = await _context.promociones
                .Where(p => !p.fecha_fin.HasValue || p.fecha_fin >= hoy)
                .ToListAsync();

            ViewBag.Platos = await _context.platos.ToDictionaryAsync(p => p.id_plato);
            ViewBag.Combos = await _context.combos.ToDictionaryAsync(c => c.id_combo);
            ViewBag.Bebidas = await _context.bebidas.ToDictionaryAsync(b => b.id_bebida, b => b.nombre);

            return View(promociones);
        }




        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var promociones = await _context.promociones.FirstOrDefaultAsync(m => m.id_promocion == id);
            if (promociones == null) return NotFound();

            return View(promociones);
        }

        public IActionResult Create()
        {
            ViewBag.Platos = new SelectList(_context.platos, "id_plato", "nombre");
            ViewBag.Combos = new SelectList(_context.combos, "id_combo", "nombre");
            ViewBag.Bebidas = new SelectList(_context.bebidas, "id_bebida", "nombre");

            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);
            ViewBag.PreciosBebidas = _context.bebidas.ToDictionary(b => b.id_bebida.ToString(), b => b.precio);

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promociones promociones)
        {
            if (!promociones.id_plato.HasValue && !promociones.id_combo.HasValue && !promociones.id_bebida.HasValue)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un plato, combo o bebida");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Platos = new SelectList(_context.platos, "id_plato", "nombre", promociones.id_plato);
                ViewBag.Combos = new SelectList(_context.combos, "id_combo", "nombre", promociones.id_combo);
                ViewBag.Bebidas = new SelectList(_context.bebidas, "id_bebida", "nombre", promociones.id_bebida);

                ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
                ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);
                ViewBag.PreciosBebidas = _context.bebidas.ToDictionary(b => b.id_bebida.ToString(), b => b.precio);

                return View(promociones);
            }

            _context.Add(promociones);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promociones = await _context.promociones.FindAsync(id);
            if (promociones == null) return NotFound();

            ViewBag.Platos = new SelectList(_context.platos, "id_plato", "nombre", promociones.id_plato);
            ViewBag.Combos = new SelectList(_context.combos, "id_combo", "nombre", promociones.id_combo);
            ViewBag.Bebidas = new SelectList(_context.bebidas, "id_bebida", "nombre", promociones.id_bebida);

            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);
            ViewBag.PreciosBebidas = _context.bebidas.ToDictionary(b => b.id_bebida.ToString(), b => b.precio);

            return View(promociones);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promociones promociones, int descuento)
        {
            if (id != promociones.id_promocion) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    decimal totalBase = 0;

                    if (promociones.id_plato.HasValue)
                        totalBase += _context.platos.FirstOrDefault(p => p.id_plato == promociones.id_plato)?.precio ?? 0;

                    if (promociones.id_combo.HasValue)
                        totalBase += _context.combos.FirstOrDefault(c => c.id_combo == promociones.id_combo)?.precio ?? 0;

                    if (promociones.id_bebida.HasValue)
                        totalBase += _context.bebidas.FirstOrDefault(b => b.id_bebida == promociones.id_bebida)?.precio ?? 0;

                    promociones.precio = totalBase - (totalBase * descuento / 100M);

                    _context.Update(promociones);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.promociones.Any(e => e.id_promocion == promociones.id_promocion))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Platos = new SelectList(_context.platos, "id_plato", "nombre", promociones.id_plato);
            ViewBag.Combos = new SelectList(_context.combos, "id_combo", "nombre", promociones.id_combo);
            ViewBag.Bebidas = new SelectList(_context.bebidas, "id_bebida", "nombre", promociones.id_bebida);

            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);
            ViewBag.PreciosBebidas = _context.bebidas.ToDictionary(b => b.id_bebida.ToString(), b => b.precio);

            return View(promociones);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var promo = await _context.promociones.FirstOrDefaultAsync(m => m.id_promocion == id);
            if (promo == null) return NotFound();

            ViewBag.NombrePlato = promo.id_plato.HasValue
                ? _context.platos.FirstOrDefault(p => p.id_plato == promo.id_plato)?.nombre
                : null;

            ViewBag.NombreCombo = promo.id_combo.HasValue
                ? _context.combos.FirstOrDefault(c => c.id_combo == promo.id_combo)?.nombre
                : null;

            ViewBag.NombreBebida = promo.id_bebida.HasValue
                ? _context.bebidas.FirstOrDefault(b => b.id_bebida == promo.id_bebida)?.nombre
                : null;

            return View(promo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promociones = await _context.promociones.FindAsync(id);
            if (promociones != null)
            {
                _context.promociones.Remove(promociones);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
