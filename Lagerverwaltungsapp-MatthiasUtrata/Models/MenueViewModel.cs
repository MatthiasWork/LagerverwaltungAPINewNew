namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MenueViewModel
    {
        // Rollenbasiert: Stammdaten, Übersichten und Benutzerverwaltung
        public bool IstAdmin { get; set; }

        // Zuständigkeitsbasiert: IDs der Räume, für die die angemeldete Person verantwortlich ist (leer = kein Raum).
        // Die Raumart spielt dafür keine Rolle
        public List<string> RaumIDs { get; set; } = new List<string>();

        // Zahl der offenen Freigaben für das Badge neben "Freigaben" (Seitenleiste und Tab-Leiste)
        public int OffeneUebernahmen { get; set; }

        // Ob die Person für mindestens einen Raum verantwortlich ist (Mein Raum, Wareneingang, Aus dem Lager)
        public bool HatRaum => RaumIDs.Count > 0;

        // "Freigaben" sieht, wer einen Raum hat, und jeder Admin, da er für die Lager freigibt (Raum.DarfBuchen)
        public bool ZeigtFreigaben => HatRaum || IstAdmin;

        // "Rückgabe" sehen alle außer der Person, die nur für das Umbuchungslager verantwortlich ist (dort kommen die Rückgaben an)
        public bool ZeigtRueckgabe => !HatRaum || RaumIDs.Any(r => !Raum.IstUmbuchungslager(r));
    }
}
