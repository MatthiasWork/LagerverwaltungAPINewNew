using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs;

// Ein Admin legt einen Benutzer mit denselben Angaben wie bei der Registrierung an und kann gleich die Rolle mitgeben
public class BenutzerAnlegenDTO : RegistrierenDTO
{
    // Ohne Angabe bekommt der Benutzer wie bei der Registrierung die Standardrolle ohne Adminrechte
    [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Rolle auswählen.")]
    public int? RolleId { get; set; }
}
