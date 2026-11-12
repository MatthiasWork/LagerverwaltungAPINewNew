using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Rückgabe (Rueckgabe/Index): Geräte aus dem eigenen Raum wie in einer Checkliste abhaken und an das Umbuchungslager zurückgeben
    public class RueckgabeViewModel
    {
        // Der eigene Raum, aus dem zurückgegeben wird (bei mehreren Räumen oben wählbar)
        public string? VonRaumID { get; set; }

        // Abgehakte Mengen je Gegenstand (Schlüssel = GegenstandID), im Formular als Mengen[GegenstandID].
        // Ein Gerät mit Seriennummer schickt 1, nicht abgehakte Geräte schicken nichts
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Der gewählte eigene Raum (mit Raumart) oder null, wenn die Person für keinen Raum verantwortlich ist
        [ValidateNever]
        public Raum? VonRaum { get; set; }

        // Die eigenen Räume, aus denen zurückgegeben werden kann (ohne das Umbuchungslager). Bei mehr als einem zeigt die Seite die Raumauswahl
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Das Umbuchungslager (mit verantwortlicher Person) oder null, wenn es den Raum nicht gibt
        [ValidateNever]
        public Raum? Umbuchungslager { get; set; }

        // Bestand des eigenen Raums, also alles, was zurückgegeben werden kann
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Ob die Person zurückgeben kann: Sie hat einen Raum, er ist nicht selbst das Umbuchungslager,
        // und im Umbuchungslager ist jemand verantwortlich, der die Rückgabe bestätigt
        public bool KannZurueckgeben => VonRaum != null && Umbuchungslager?.Person != null && VonRaum.ID != Umbuchungslager.ID;
    }
}
