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
    public class PlatosController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public PlatosController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        // GET: Platos
        public async Task<IActionResult> Index()
        {
            return View(await _context.platos.ToListAsync());
        }

        // GET: Platos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var platos = await _context.platos
                .FirstOrDefaultAsync(m => m.id_plato == id);
            if (platos == null)
            {
                return NotFound();
            }

            return View(platos);
        }

        // GET: Platos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Platos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_plato,nombre,precio")] Platos platos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(platos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(platos);
        }

        // GET: Platos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var platos = await _context.platos.FindAsync(id);
            if (platos == null)
            {
                return NotFound();
            }
            return View(platos);
        }

        // POST: Platos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_plato,nombre,precio")] Platos platos)
        {
            if (id != platos.id_plato)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(platos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlatosExists(platos.id_plato))
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
            return View(platos);
        }

        // GET: Platos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var platos = await _context.platos
                .FirstOrDefaultAsync(m => m.id_plato == id);
            if (platos == null)
            {
                return NotFound();
            }

            return View(platos);
        }

        // POST: Platos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var platos = await _context.platos.FindAsync(id);
            if (platos != null)
            {
                _context.platos.Remove(platos);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlatosExists(int id)
        {
            return _context.platos.Any(e => e.id_plato == id);
        }
    }
}
