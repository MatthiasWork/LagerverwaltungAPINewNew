using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class LoginViewModel
    {
        // Wie bei Person; so landen keine beliebig langen Schlüssel im LoginSperreService.
        [Required(ErrorMessage = "Bitte Benutzernamen eingeben.")]
        [StringLength(20, ErrorMessage = "Der Benutzername darf höchstens {1} Zeichen lang sein.")]
        [Display(Name = "Benutzername")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = null!;
    }
}
