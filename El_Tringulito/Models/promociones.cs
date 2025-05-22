using System;
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

        [Column("estado")]
        public string estado { get; set; } = "activa";  // activa o vencida

        // Relación con Plato (opcional)
        [ForeignKey("id_plato")]
        public virtual Platos Plato { get; set; }

        // Relación con Combo (opcional)
        [ForeignKey("id_combo")]
        public virtual combos Combo { get; set; }
    }
}
