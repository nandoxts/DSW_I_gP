// Models/Usuario.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = "";

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = "";

        [Required, MaxLength(50)]
        public string Rol { get; set; } = "";

        public ICollection<Orden>? Ordenes { get; set; }
    }
}
