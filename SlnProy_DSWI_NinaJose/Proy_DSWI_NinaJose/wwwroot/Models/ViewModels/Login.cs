using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class Login
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
