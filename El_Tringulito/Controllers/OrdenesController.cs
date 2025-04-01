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
            return View(await _context.ordenes.ToListAsync());
        }

        // GET: Ordenes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordenes = await _context.ordenes
                .FirstOrDefaultAsync(m => m.id_orden == id);
            if (ordenes == null)
            {
                return NotFound();
            }

            return View(ordenes);
        }

        // GET: Ordenes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ordenes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,nombre_cliente,fecha,estado,comentario,total")] Ordenes ordenes)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ordenes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ordenes);
        }

        // GET: Ordenes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordenes = await _context.ordenes.FindAsync(id);
            if (ordenes == null)
            {
                return NotFound();
            }
            return View(ordenes);
        }

        // POST: Ordenes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_orden,id_mesa,id_plato,id_promocion,id_combo,nombre_cliente,fecha,estado,comentario,total")] Ordenes ordenes)
        {
            if (id != ordenes.id_orden)
            {
                return NotFound();
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
                    if (!OrdenesExists(ordenes.id_orden))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ordenes);
        }

        // GET: Ordenes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ordenes = await _context.ordenes
                .FirstOrDefaultAsync(m => m.id_orden == id);
            if (ordenes == null)
            {
                return NotFound();
            }

            return View(ordenes);
        }

        // POST: Ordenes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ordenes = await _context.ordenes.FindAsync(id);
            if (ordenes != null)
            {
                _context.ordenes.Remove(ordenes);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrdenesExists(int id)
        {
            return _context.ordenes.Any(e => e.id_orden == id);
        }
    }
}
