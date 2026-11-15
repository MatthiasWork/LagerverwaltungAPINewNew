namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel des Katalogs.
    public class GegenstandUebersichtViewModel
    {
        // Die möglichen Status eines Gegenstands.
        public const string Verfuegbar = "Verfügbar";
        public const string InAusleihe = "In Ausleihe";
        public const string KeinBestand = "Kein Bestand";

        // Die gefilterte Liste der Gegenstände.
        public List<KatalogEintrag> Eintraege { get; set; } = new List<KatalogEintrag>();

        // Die Anzahl aller Gegenstände und Räume.
        public int AnzahlGesamt { get; set; }

        public int AnzahlRaeume { get; set; }

        // Die aktuellen Filterwerte.
        public string? Suche { get; set; }

        public int? KategorieID { get; set; }

        public int? HerstellerID { get; set; }

        public string? RaumID { get; set; }

        public string? Status { get; set; }

        // Gibt an, ob ein Filter gesetzt ist.
        public bool Gefiltert => !string.IsNullOrEmpty(Suche) || KategorieID != null || HerstellerID != null
            || !string.IsNullOrEmpty(RaumID) || !string.IsNullOrEmpty(Status);

        // Alle Kategorien für den Filter.
        public List<Kategorie> Kategorien { get; set; } = new List<Kategorie>();

        // Die Hersteller für den Filter; nur geladen, wenn nach einem Hersteller gefiltert wird.
        public List<Hersteller> Hersteller { get; set; } = new List<Hersteller>();

        // Die IDs aller Räume für den Filter.
        public List<string> Raeume { get; set; } = new List<string>();
    }

    // Eine Zeile im Katalog mit Gegenstand, Standort und Status.
    public class KatalogEintrag
    {
        public Gegenstand Gegenstand { get; set; } = null!;

        // Die Räume, in denen der Gegenstand liegt.
        public List<Raumbestand> Standorte { get; set; } = new List<Raumbestand>();

        // Der erste Raum oder null, wenn der Gegenstand in keinem Raum liegt.
        public Raum? ErsterRaum => Standorte.FirstOrDefault()?.Raum;

        // Der Standort für die Anzeige, null, wenn der Gegenstand in keinem Raum liegt.
        public string? Standort
        {
            get
            {
                if (ErsterRaum == null)
                {
                    return null;
                }

                var text = $"{ErsterRaum.ID} · {ErsterRaum.Raumart.Name}";
                if (Standorte.Count > 1)
                {
                    text += $" +{Standorte.Count - 1}";
                }
                return text;
            }
        }

        // Die Stückzahl in allen Räumen zusammen.
        public int Stueck { get; set; }

        public string Status { get; set; } = null!;

        // Ein Zusatz zum Status, z. B. wohin der Gegenstand unterwegs ist.
        public string? Hinweis { get; set; }
    }
}
