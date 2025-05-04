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

        private bool OrdenesExists(int id)
        {
            return _context.ordenes.Any(e => e.id_orden == id);
        }
    }
}