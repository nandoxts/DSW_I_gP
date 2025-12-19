using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class Register
    {
        [Required]
        public string Nombre { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}
