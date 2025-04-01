using Microsoft.AspNetCore.Mvc;
using El_Tringulito.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace El_Tringulito.Controllers
{
    public class MesasMeseroController : Controller
    {
        private readonly ElTriangulitoDBContext _context;

        public MesasMeseroController(ElTriangulitoDBContext context)
        {
            _context = context;
        }

        // Vista principal de mesas
        public IActionResult Index()
        {
            var mesas = _context.mesas.ToList();
            return View(mesas);
        }

        // Vista para reservar una mesa
        public IActionResult Reservar(int id)
        {
            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id);
            if (mesa == null || mesa.estado != "Libre")
            {
                return NotFound();
            }

            return View(mesa);
        }

        // Método para obtener platos (usado en el modal)
        public async Task<IActionResult> GetPlatos()
        {
            var platos = await _context.platos.ToListAsync();
            return Json(platos);
        }

        // Método para obtener promociones (usado en el modal)
        public async Task<IActionResult> GetPromociones()
        {
            var promociones = await _context.promociones.ToListAsync();
            return Json(promociones);
        }

        // Método para obtener combos (usado en el modal)
        public async Task<IActionResult> GetCombos()
        {
            var combos = await _context.combos.ToListAsync();
            return Json(combos);
        }

        // Método para crear la orden
        [HttpPost]
        public IActionResult CrearOrden(int id_mesa, string nombre_cliente, string comentario, decimal total, List<ProductoSeleccionado> productos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (productos == null || !productos.Any())
            {
                Console.WriteLine("❌ No se recibieron productos en la orden.");
                return BadRequest("No se seleccionaron productos.");
            }

            Console.WriteLine($"✅ Datos recibidos: Mesa: {id_mesa}, Cliente: {nombre_cliente}, Total: {total}");
            Console.WriteLine("🛒 Productos seleccionados:");
            foreach (var producto in productos)
            {
                Console.WriteLine($"- ID: {producto.id}, Tipo: {producto.tipo}");
            }

            foreach (var producto in productos)
            {
                var nuevaOrden = new Ordenes
                {
                    id_mesa = id_mesa,
                    nombre_cliente = nombre_cliente,
                    comentario = comentario,
                    fecha = DateTime.Now,
                    estado = "Pendiente",
                    total = total
                };

                // Asignar el ID del producto según su tipo
                if (producto.tipo == "platos")
                {
                    nuevaOrden.id_plato = producto.id;
                }
                else if (producto.tipo == "promociones")
                {
                    nuevaOrden.id_promocion = producto.id;
                }
                else if (producto.tipo == "combos")
                {
                    nuevaOrden.id_combo = producto.id;
                }

                _context.ordenes.Add(nuevaOrden);
            }

            // Guardar las órdenes en la base de datos
            _context.SaveChanges();
            Console.WriteLine("✅ Orden guardada correctamente.");

            // Cambiar el estado de la mesa a "Ocupada"
            var mesa = _context.mesas.FirstOrDefault(m => m.id_mesa == id_mesa);
            if (mesa != null)
            {
                mesa.estado = "Ocupada";
                _context.SaveChanges();
                Console.WriteLine($"✅ Mesa {id_mesa} ahora está 'Ocupada'.");
            }

            return RedirectToAction("Index");
        }


        public class ProductoSeleccionado
        {
            public int id { get; set; }
            public string tipo { get; set; }
        }
    }
}