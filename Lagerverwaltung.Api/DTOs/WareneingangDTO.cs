using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class WareneingangDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Artikel auswählen.")]
        public int GegenstandId { get; set; }

        [Range(1, 100000, ErrorMessage = "Die Menge muss zwischen {1} und {2} liegen.")]
        public int Menge { get; set; }

        [StringLength(8, ErrorMessage = "Die Raumnummer darf höchstens {1} Zeichen lang sein.")]
        public string? RaumId { get; set; }
    }
}
