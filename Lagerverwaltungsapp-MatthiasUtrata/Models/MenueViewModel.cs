namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MenueViewModel
    {
        // Gibt an, ob die Person Admin ist.
        public bool IstAdmin { get; set; }

        // Die IDs der Räume, für die die angemeldete Person verantwortlich ist.
        public List<string> RaumIDs { get; set; } = new List<string>();

        // Die Anzahl der offenen Freigaben.
        public int OffeneUebernahmen { get; set; }

        // Gibt an, ob die Person für mindestens einen Raum verantwortlich ist.
        public bool HatRaum => RaumIDs.Count > 0;

        // Gibt an, ob "Freigaben" angezeigt wird; das sehen alle mit einem Raum und jeder Admin.
        public bool ZeigtFreigaben => HatRaum || IstAdmin;

        // Gibt an, ob "Rückgabe" angezeigt wird; das sehen alle außer wer nur für das Umbuchungslager verantwortlich ist.
        public bool ZeigtRueckgabe => !HatRaum || RaumIDs.Any(r => !Raum.IstUmbuchungslager(r));
    }
}
