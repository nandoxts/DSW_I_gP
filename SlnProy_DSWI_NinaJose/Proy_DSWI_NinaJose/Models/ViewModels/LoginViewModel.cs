using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class Login
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
