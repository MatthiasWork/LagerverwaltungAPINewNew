namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel der Historie.
    public class HistorieViewModel
    {
        // Die möglichen Status einer Lagerbewegung.
        public const string FreigabeOffen = "Freigabe offen";
        public const string Bestaetigt = "Bestätigt";
        public const string Storniert = "Storniert";

        // Die Anzahl der Lagerbewegungen je Seite.
        public const int EintraegeJeSeite = 50;

        // Die Lagerbewegungen der aktuellen Seite, die neueste zuerst.
        public List<Lagerbewegung> Lagerbewegungen { get; set; } = new List<Lagerbewegung>();

        // Die Anzahl aller und die Anzahl der gefilterten Lagerbewegungen.
        public int AnzahlGesamt { get; set; }

        public int AnzahlGefiltert { get; set; }

        // Die aktuelle Seite (ab 1) und die Anzahl der Seiten.
        public int Seite { get; set; }

        public int AnzahlSeiten { get; set; }

        // Die aktuellen Filterwerte.
        public string? Suche { get; set; }

        public string? RaumID { get; set; }

        public int? BewegungsartID { get; set; }

        public string? Status { get; set; }

        public DateTime? Von { get; set; }

        public DateTime? Bis { get; set; }

        // Gibt an, ob ein Filter gesetzt ist.
        public bool Gefiltert => !string.IsNullOrEmpty(Suche) || !string.IsNullOrEmpty(RaumID) || BewegungsartID != null
            || !string.IsNullOrEmpty(Status) || Von != null || Bis != null;

        // Die IDs aller Räume für den Filter.
        public List<string> Raeume { get; set; } = new List<string>();

        // Alle Bewegungsarten für den Filter.
        public List<Bewegungsart> Bewegungsarten { get; set; } = new List<Bewegungsart>();

        /// <summary>
        /// Methode, die den Status einer Lagerbewegung für die Anzeige liefert. Die Bewegungsart muss geladen sein.
        /// </summary>
        /// <param name="lagerbewegung">Die Lagerbewegung mit Bewegungsart.</param>
        /// <returns>Der Status: Freigabe offen, Bestätigt oder Storniert.</returns>
        public static string StatusVon(Lagerbewegung lagerbewegung)
        {
            if (lagerbewegung.Offen)
            {
                return FreigabeOffen;
            }
            if (lagerbewegung.Storniert)
            {
                return Storniert;
            }
            return Bestaetigt;
        }
    }
}
