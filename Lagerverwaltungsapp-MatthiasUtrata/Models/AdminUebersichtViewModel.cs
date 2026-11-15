namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel der Benutzerverwaltung.
    public class AdminUebersichtViewModel
    {
        // Die Anzahl aller Benutzer und die Anzahl je Rolle (Schlüssel ist die RolleID).
        public int AnzahlBenutzer { get; set; }

        public Dictionary<int, int> AnzahlJeRolle { get; set; } = new Dictionary<int, int>();

        public List<Rolle> Rollen { get; set; } = new List<Rolle>();

        // Die gefilterte Benutzerliste.
        public List<Person> Benutzer { get; set; } = new List<Person>();

        // Die noch nicht übernommenen Ausleihen je Person (Schlüssel ist die PersonID).
        public Dictionary<int, int> OffeneAusleihen { get; set; } = new Dictionary<int, int>();

        // Der Benutzer, der im Detail angezeigt wird.
        public Person? Ausgewaehlt { get; set; }

        // Die aktuellen Filterwerte.
        public string? Suche { get; set; }

        public int? RolleID { get; set; }

        // Die ID des angemeldeten Benutzers.
        public int? AngemeldeteBenutzerID { get; set; }
    }
}
