using System.ComponentModel.DataAnnotations;

namespace CI.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}