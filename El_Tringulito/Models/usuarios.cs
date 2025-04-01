using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace El_Tringulito.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("usuario")] 
    public string NombreUsuario { get; set; }

    [Required]
    [StringLength(255)]
    [Column("contrasenia")] 
        public string Contrasenia { get; set; }
    }
}

