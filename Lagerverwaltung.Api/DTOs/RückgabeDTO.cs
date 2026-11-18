using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RückgabeDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Bitte einen Artikel auswählen.")]
        public int GegenstandId { get; set; }

        [Range(1, 100000, ErrorMessage = "Die Menge muss zwischen {1} und {2} liegen.")]
        public int Menge { get; set; }

        [Required(ErrorMessage = "Bitte das Labor angeben, aus dem zurückgegeben wird.")]
        [StringLength(8, ErrorMessage = "Die Raumnummer darf höchstens {1} Zeichen lang sein.")]
        public string VonRaumId { get; set; } = null!;
    }
}
