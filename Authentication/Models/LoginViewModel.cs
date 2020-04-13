using Authentication.Resources;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessageResourceName = nameof(Strings.EmailError), ErrorMessageResourceType = typeof(Strings))]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = nameof(Strings.PasswordError), ErrorMessageResourceType = typeof(Strings))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }
}
