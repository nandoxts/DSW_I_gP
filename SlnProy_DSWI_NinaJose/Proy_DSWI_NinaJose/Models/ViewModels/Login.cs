using System.ComponentModel.DataAnnotations;

namespace Proy_DSWI_NinaJose.Models
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
