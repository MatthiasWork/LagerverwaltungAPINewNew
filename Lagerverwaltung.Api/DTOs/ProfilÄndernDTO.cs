using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs;

// Das eigene Profil hat dieselben Angaben wie BenutzerÄndernDTO, ein neues Passwort braucht aber zusätzlich das aktuelle
public class ProfilÄndernDTO : BenutzerÄndernDTO
{
    [StringLength(128, ErrorMessage = "Das Passwort darf höchstens {1} Zeichen lang sein.")]
    public string? AktuellesPasswort { get; set; }
}
