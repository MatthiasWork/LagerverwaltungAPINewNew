using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RolleÄndernDTO
    {
        // Range schlägt fehl, wenn das Feld für die RolleId leer ist
        [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Rolle auswählen.")]
        public int RolleId { get; set; }
    }
}
