using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace El_Tringulito.Hubs
{
    public class CocinaHub : Hub
    {
        // Método para notificar a cocina sobre una nueva orden creada
        public async Task NotificarNuevaOrdenCreada(int mesaId)
        {
            await Clients.All.SendAsync("NuevaOrdenCreada", mesaId);
        }

        // Método para notificar a cocina sobre productos agregados a una orden existente
        public async Task NotificarNuevaOrdenAgregada(int mesaId)
        {
            await Clients.All.SendAsync("NuevaOrdenAgregada", mesaId);
        }
    }
}
