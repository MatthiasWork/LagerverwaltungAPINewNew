using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class AusborgenViewModel
    {
        // Der Raum, aus dem verliehen wird: ein eigener Raum oder, für einen Admin, ein Lager (Raum.DarfBuchen).
        // Leer heißt der erste eigene Raum
        public string? VonRaumID { get; set; }

        // Der Raum, an den ausgeborgt wird. Die dort verantwortliche Person muss die Übernahme bestätigen
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Verleihen an Raum")]
        public string? NachRaumID { get; set; }

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Eingegebene Mengen je Gegenstand (Schlüssel = GegenstandID), im Formular als Mengen[GegenstandID].
        // Ein Gerät mit Seriennummer schickt über die Checkbox 1, ein leeres Feld oder 0 heißt "nicht ausborgen"
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Der Raum (mit Raumart und verantwortlicher Person), aus dem ausgeborgt wird
        [ValidateNever]
        public Raum VonRaum { get; set; } = null!;

        // Die Räume, aus denen die Person verleihen darf: die eigenen und für einen Admin alle Lager. Bei mehr als einem
        // zeigt die Seite die Auswahl "Verleihen aus"
        [ValidateNever]
        public List<Raum> VonRaeume { get; set; } = new List<Raum>();

        // Die Räume, für die die angemeldete Person verantwortlich ist (leer = keiner)
        [ValidateNever]
        public List<string> EigeneRaumIDs { get; set; } = new List<string>();

        // Bestand des Von-Raums, also alles, was ausgeborgt werden kann
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Mögliche Zielräume mit Raumart und verantwortlicher Person (für die Auswahl und die Zusammenfassung)
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
