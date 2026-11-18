using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RaumAnlegenDTO
    {
        [Required(ErrorMessage = "Bitte eine Raumnummer eingeben.")]
        [StringLength(8, ErrorMessage = "Die Raumnummer darf höchstens {1} Zeichen lang sein.")]
        public string Id { get; set; } = null!;

        public int? PersonId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Raumart auswählen.")]
        public int RaumartId { get; set; }
    }
}
