using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs;

// Ein Admin legt einen Benutzer mit denselben Angaben wie bei der Registrierung an und kann gleich die Rolle mitgeben
public class BenutzerAnlegenDTO
{
    [Required(ErrorMessage = "Bitte Vornamen eingeben.")]
    [StringLength(250, ErrorMessage = "Der Vorname darf höchstens {1} Zeichen lang sein.")]
    public string Vorname { get; set; } = null!;

    [Required(ErrorMessage = "Bitte Nachnamen eingeben.")]
    [StringLength(250, ErrorMessage = "Der Nachname darf höchstens {1} Zeichen lang sein.")]
    public string Nachname { get; set; } = null!;

    [Required(ErrorMessage = "Bitte Benutzernamen eingeben.")]
    [StringLength(20, ErrorMessage = "Der Benutzername darf höchstens {1} Zeichen lang sein.")]
    public string Benutzername { get; set; } = null!;

    [Required(ErrorMessage = "Bitte E-Mail-Adresse eingeben.")]
    [EmailAddress(ErrorMessage = "Bitte eine gültige E-Mail-Adresse eingeben.")]
    [StringLength(100, ErrorMessage = "Die E-Mail-Adresse darf höchstens {1} Zeichen lang sein.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Bitte Passwort eingeben.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Das Passwort muss zwischen {2} und {1} Zeichen lang sein.")]
    public string Passwort { get; set; } = null!;

    // Ohne Angabe bekommt der Benutzer wie bei der Registrierung die Standardrolle ohne Adminrechte
    [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Rolle auswählen.")]
    public int? RolleId { get; set; }
}
