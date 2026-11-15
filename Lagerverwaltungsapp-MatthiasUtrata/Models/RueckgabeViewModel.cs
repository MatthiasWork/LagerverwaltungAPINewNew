using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel zum Zurückgeben von Geräten aus dem eigenen Raum an das Umbuchungslager.
    public class RueckgabeViewModel
    {
        // Der eigene Raum, aus dem zurückgegeben wird.
        public string? VonRaumID { get; set; }

        // Die abgehakten Mengen je Gegenstand (Schlüssel ist die GegenstandID).
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige; diese Werte kommen nicht aus dem Formular und werden nicht validiert.

        // Der gewählte eigene Raum oder null, wenn die Person für keinen Raum verantwortlich ist.
        [ValidateNever]
        public Raum? VonRaum { get; set; }

        // Die eigenen Räume, aus denen zurückgegeben werden kann, ohne das Umbuchungslager.
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Das Umbuchungslager oder null, wenn es den Raum nicht gibt.
        [ValidateNever]
        public Raum? Umbuchungslager { get; set; }

        // Der Bestand des eigenen Raums.
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Gibt an, ob die Person zurückgeben kann: Sie hat einen Raum, der nicht das Umbuchungslager ist, und dort ist jemand verantwortlich.
        public bool KannZurueckgeben => VonRaum != null && Umbuchungslager?.Person != null && VonRaum.ID != Umbuchungslager.ID;
    }
}
