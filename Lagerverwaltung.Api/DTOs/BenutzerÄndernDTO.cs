using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs;

// Die Angaben, die ein Admin bei jedem Benutzer ändern darf. Die Rolle wird über PUT api/users/{id}/role geändert
public class BenutzerÄndernDTO
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

    // Ohne Angabe bleibt das bisherige Passwort erhalten
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Das neue Passwort muss zwischen {2} und {1} Zeichen lang sein.")]
    public string? NeuesPasswort { get; set; }
}
