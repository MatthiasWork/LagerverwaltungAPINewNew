using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RückgabeDTO
    {
        // Range schlägt fehl, wenn das Feld für die GegenstandId leer ist
        [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Artikel auswählen.")]
        public int GegenstandId { get; set; }

        // Range schlägt fehl, wenn das Feld für die Menge leer ist
        [Range(1, int.MaxValue, ErrorMessage = "Die Menge muss zwischen {1} und {2} liegen.")]
        public int Menge { get; set; }

        [Required(ErrorMessage = "Bitte angeben von welchem Raum die Rückgabe kommt.")]
        [StringLength(8, ErrorMessage = "Die Raumnummer darf höchstens {1} Zeichen lang sein.")]
        public string VonRaumId { get; set; } = null!;
    }
}
