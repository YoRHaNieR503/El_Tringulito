using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace El_Tringulito
    .Models
{
    [Table("bebidas")]
    public class Bebidas
    {
        [Key]
        public int id_bebida { get; set; }

        public string nombre { get; set; }

        public decimal precio { get; set; }
    }
}