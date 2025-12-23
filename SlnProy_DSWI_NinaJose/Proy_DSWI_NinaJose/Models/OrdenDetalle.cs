// Models/OrdenDetalle.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_DSWI_NinaJose.Models
{
    public class OrdenDetalle
    {
        [Key]
        public int IdOrdenDetalle { get; set; }

        public int IdOrden { get; set; }
        public Orden? Orden { get; set; }

        public int IdProducto { get; set; }
        public Producto? Producto { get; set; }      // navegación hacia Producto

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }
    }
}
