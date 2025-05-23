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

            var promociones = await _context.promociones.ToListAsync();
            ViewBag.Platos = await _context.platos.ToListAsync();
            ViewBag.Combos = await _context.combos.ToListAsync();
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

            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promociones promociones)
        {
            // Validar que al menos tenga plato o combo
            if (!promociones.id_plato.HasValue && !promociones.id_combo.HasValue)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un plato o un combo");
            }

            // Verifica errores de validación
            if (!ModelState.IsValid)
            {
                // Imprime errores en la consola (para debug)
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                ViewBag.Platos = new SelectList(_context.platos, "id_plato", "nombre", promociones.id_plato);
                ViewBag.Combos = new SelectList(_context.combos, "id_combo", "nombre", promociones.id_combo);
                ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
                ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);

                return View(promociones);
            }

            // Asigna el estado por defecto (por si acaso)
            promociones.estado = "activa";

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
            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);

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
            ViewBag.PreciosPlatos = _context.platos.ToDictionary(p => p.id_plato.ToString(), p => p.precio);
            ViewBag.PreciosCombos = _context.combos.ToDictionary(c => c.id_combo.ToString(), c => c.precio);

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
