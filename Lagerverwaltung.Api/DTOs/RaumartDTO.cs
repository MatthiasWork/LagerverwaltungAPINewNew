using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RaumartDTO
    {
        [Required(ErrorMessage = "Bitte einen Namen eingeben.")]
        [StringLength(20, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")]
        public string Name { get; set; } = null!;
    }
}
