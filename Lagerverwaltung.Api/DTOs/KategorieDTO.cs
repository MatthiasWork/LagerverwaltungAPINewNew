using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class KategorieDTO
    {
        [Required(ErrorMessage = "Bitte einen Namen eingeben.")]
        [StringLength(30, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")]
        public string Name { get; set; } = null!;
    }
}
