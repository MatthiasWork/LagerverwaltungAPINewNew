using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel zum Korrigieren des Bestands eines Raums.
    public class KorrekturViewModel
    {
        // Der Raum, dessen Bestand korrigiert wird.
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Raum")]
        public string? RaumID { get; set; }

        // Die neuen Mengen je Gegenstand (Schlüssel ist die GegenstandID).
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Die Mengen beim Anzeigen, damit der LagerService erkennt, ob sich der Bestand inzwischen geändert hat.
        public Dictionary<int, int> Bisher { get; set; } = new Dictionary<int, int>();

        // Ein Gegenstand, der noch nicht im Raum ist, mit seiner Menge.
        [Display(Name = "Gegenstand hinzufügen")]
        public int? NeuGegenstandID { get; set; }

        [Display(Name = "Menge")]
        public int? NeuMenge { get; set; }

        // Ab hier nur für die Anzeige; diese Werte kommen nicht aus dem Formular und werden nicht validiert.

        // Alle Räume zur Auswahl.
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Der gewählte Raum oder null, wenn noch keiner gewählt ist.
        [ValidateNever]
        public Raum? Raum { get; set; }

        // Der Bestand des gewählten Raums.
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Die Gegenstände, die hinzugefügt werden können.
        [ValidateNever]
        public List<Gegenstand> Hinzufuegbar { get; set; } = new List<Gegenstand>();
    }
}
