using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class ÜbergabeDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Artikel auswählen.")]
        public int GegenstandId { get; set; }

        [Range(1, 100000, ErrorMessage = "Die Menge muss zwischen {1} und {2} liegen.")]
        public int Menge { get; set; }

        [Required(ErrorMessage = "Bitte das Ziel-Labor angeben.")]
        [StringLength(8, ErrorMessage = "Die Raumnummer darf höchstens {1} Zeichen lang sein.")]
        public string NachRaumId { get; set; } = null!;
    }
}
