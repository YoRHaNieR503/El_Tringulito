using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace El_Tringulito.Models
{
    [Table("ordenes")]
    public class Ordenes
    {
        [Key]
        public int id_orden { get; set; }

        public int id_mesa { get; set; }

        public int? id_plato { get; set; } // Asegúrate de que sea nullable

        public int? id_promocion { get; set; } // Asegúrate de que sea nullable

        public int? id_combo { get; set; } // Asegúrate de que sea nullable

        public string? nombre_cliente { get; set; }

        public DateTime fecha { get; set; }

        public string? estado { get; set; }

        public string? comentario { get; set; }

        public decimal? total { get; set; }

    }
}