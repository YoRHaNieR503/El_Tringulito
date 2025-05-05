using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using El_Tringulito.Models;
using El_Tringulito.Helpers;

namespace El_Tringulito.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public UsuariosController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            return View(await _context.usuarios.ToListAsync());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();

            usuario.Contrasenia = "********";
            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NombreUsuario,Contrasenia,Rol")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                if (usuario.Contrasenia.Length < 8)
                {
                    ModelState.AddModelError("Contrasenia", "La contraseña debe tener al menos 8 caracteres");
                    return View(usuario);
                }

                usuario.Contrasenia = PasswordHelper.HashPassword(usuario.Contrasenia);
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Contrasenia = "";
            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NombreUsuario,Contrasenia,Rol")] Usuario usuario)
        {
            if (id != usuario.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

                    if (existingUser == null) return NotFound();

                    if (string.IsNullOrEmpty(usuario.Contrasenia))
                    {
                        usuario.Contrasenia = existingUser.Contrasenia;
                    }
                    else
                    {
                        if (usuario.Contrasenia.Length < 8)
                        {
                            ModelState.AddModelError("Contrasenia", "La contraseña debe tener al menos 8 caracteres");
                            return View(usuario);
                        }

                        usuario.Contrasenia = PasswordHelper.HashPassword(usuario.Contrasenia);
                    }

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.usuarios.FirstOrDefaultAsync(m => m.Id == id);
            if (usuario == null) return NotFound();

            usuario.Contrasenia = "********";
            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.usuarios.Any(e => e.Id == id);
        }
    }
}
