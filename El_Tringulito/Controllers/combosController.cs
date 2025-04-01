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
    public class combosController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public combosController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        // GET: combos 
        public async Task<IActionResult> Index()
        {
            return View(await _context.combos.ToListAsync());
        }

        // GET: combos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combos = await _context.combos
                .FirstOrDefaultAsync(m => m.id_combo == id);
            if (combos == null)
            {
                return NotFound();
            }

            return View(combos);
        }

        // GET: combos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: combos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_combo,nombre,precio")] combos combos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(combos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(combos);
        }

        // GET: combos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combos = await _context.combos.FindAsync(id);
            if (combos == null)
            {
                return NotFound();
            }
            return View(combos);
        }

        // POST: combos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_combo,nombre,precio")] combos combos)
        {
            if (id != combos.id_combo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(combos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!combosExists(combos.id_combo))
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
            return View(combos);
        }

        // GET: combos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combos = await _context.combos
                .FirstOrDefaultAsync(m => m.id_combo == id);
            if (combos == null)
            {
                return NotFound();
            }

            return View(combos);
        }

        // POST: combos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var combos = await _context.combos.FindAsync(id);
            if (combos != null)
            {
                _context.combos.Remove(combos);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool combosExists(int id)
        {
            return _context.combos.Any(e => e.id_combo == id);
        }
    }
}
