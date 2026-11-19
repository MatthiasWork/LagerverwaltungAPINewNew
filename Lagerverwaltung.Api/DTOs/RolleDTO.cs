using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RolleDTO
    {
        [Required(ErrorMessage = "Bitte einen Namen eingeben.")]
        [StringLength(50, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")]
        public string Name { get; set; } = null!;

        // Ohne Angabe hat die Rolle keine Adminrechte
        public bool Admin { get; set; }
    }
}
