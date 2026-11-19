using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.DTOs
{
    public class RaumbestandÄndernDTO
    {
        // Range schlägt fehl, wenn das Feld für die Menge leer ist
        // Soll der Artikel ganz aus dem Raum, wird die Bestandszeile gelöscht (DELETE)
        [Range(1, int.MaxValue, ErrorMessage = "Die Menge muss zwischen {1} und {2} liegen.")]
        public int Menge { get; set; }
    }
}
