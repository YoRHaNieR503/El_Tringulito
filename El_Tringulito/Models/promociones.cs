using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace El_Tringulito.Models
{
    [Table("promociones")]
    public class Promociones
    {
        [Key]
        public int id_promocion { get; set; }

        public int? id_plato { get; set; }

        public int? id_combo { get; set; }

        public DateTime? fecha_inicio { get; set; }

        public DateTime? fecha_fin { get; set; }

        public decimal? precio { get; set; }
    }
}