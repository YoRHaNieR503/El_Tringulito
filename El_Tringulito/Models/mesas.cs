using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElTriangulitoP.Models
{
    [Table("mesas")]
    public class Mesas
    {
        [Key]
        public int id_mesa { get; set; }

        public string? nombre { get; set; }

        public string? estado { get; set; }
    }
}