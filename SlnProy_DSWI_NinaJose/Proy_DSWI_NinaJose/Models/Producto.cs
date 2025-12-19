// Models/Producto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_DSWI_NinaJose.Models
{
    public class Producto
    {
        [Key]
        public int IdProducto { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        public int Stock { get; set; }

        // FK a Categoría
        public int IdCategoria { get; set; }
        public Categoria? Categoria { get; set; }

        [Required, StringLength(255)]
        public string ImagenUrl { get; set; } = "";

        // <<< Aquí añadimos la navegación inversa para OrdenDetalles >>>
        public ICollection<OrdenDetalle>? OrdenDetalles { get; set; }
    }
}
