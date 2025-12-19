// Models/Orden.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models
{
    public class Orden
    {
        [Key]
        public int IdOrden { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required, StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        // Navegación hacia Usuario
        public Usuario Usuario { get; set; } = null!;

        // ¡Aquí está la colección correcta!
        public ICollection<OrdenDetalle> OrdenDetalles { get; set; }
            = new List<OrdenDetalle>();
    }
}
