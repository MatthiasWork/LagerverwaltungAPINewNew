using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RaumÄndernDTO
    {
        public int? PersonId { get; set; }

        // Range schlägt fehl, wenn das Feld für die RaumartId leer ist
        [Range(1, int.MaxValue, ErrorMessage = "Bitte eine Raumart auswählen.")]
        public int RaumartId { get; set; }
    }
}
