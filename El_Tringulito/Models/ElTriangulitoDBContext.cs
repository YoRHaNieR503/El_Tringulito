using El_Tringulito.Models;
using Microsoft.EntityFrameworkCore;

namespace El_Tringulito.Models
{
    public class ElTriangulitoDBContext : DbContext
    {
        public ElTriangulitoDBContext(DbContextOptions<ElTriangulitoDBContext> options) : base(options)
        {
        }

        public DbSet<combos> combos { get; set; }
        public DbSet<Mesas> mesas { get; set; }
        public DbSet<Promociones> promociones { get; set; }
        public DbSet<Platos> platos { get; set; }
        public DbSet<Ordenes> ordenes { get; set; }
        public DbSet<Usuario> usuarios { get; set; }



    }
}