// Models/OrdenDetalle.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_DSWI_NinaJose.Models
{
    public class OrdenDetalle
    {
        // Esta propiedad se llama IdDetalle en tu clase,
        // pero en la base de datos la columna es IdOrdenDetalle.
        [Column("IdOrdenDetalle")]
        public int IdDetalle { get; set; }

        public int IdOrden { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Navegaciones
        public Orden Orden { get; set; } = null!;
        public Producto Producto { get; set; } = null!;
    }
}
