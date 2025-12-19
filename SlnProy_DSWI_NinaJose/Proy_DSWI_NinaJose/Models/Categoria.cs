// Models/Categoria.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = "";

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public ICollection<Producto>? Productos { get; set; }
    }
}
