using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class Register
    {
        [Required]
        public required string Nombre { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public required string ConfirmPassword { get; set; }
    }
}
