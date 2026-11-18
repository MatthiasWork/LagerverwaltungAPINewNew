using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class ArtikelDTO
    {
        [Required(ErrorMessage = "Bitte eine Bezeichnung eingeben.")]
        [StringLength(100, ErrorMessage = "Die Bezeichnung darf höchstens {1} Zeichen lang sein.")]
        public string Name { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Die Seriennummer darf höchstens {1} Zeichen lang sein.")]
        public string? Seriennummer { get; set; }

        // Fehlt das Feld im JSON, ist der Wert 0 und Range schlägt fehl.
        [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Kategorie auswählen.")]
        public int KategorieId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Hersteller auswählen.")]
        public int HerstellerId { get; set; }
    }
}
