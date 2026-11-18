using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Bitte Benutzernamen eingeben.")]
        [StringLength(20, ErrorMessage = "Der Benutzername darf höchstens {1} Zeichen lang sein.")]
        public string Benutzername { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        [StringLength(128, ErrorMessage = "Das Passwort darf höchstens {1} Zeichen lang sein.")]
        public string Passwort { get; set; } = null!;
    }
}
