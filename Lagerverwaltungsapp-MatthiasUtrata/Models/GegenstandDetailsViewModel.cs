namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Gerätedetail (Gegenstand/Details)
    public class GegenstandDetailsViewModel
    {
        // Der Gegenstand mit Standorten, Stückzahl und Status wie im Katalog
        public KatalogEintrag Eintrag { get; set; } = null!;

        // Bewegungen, die noch auf die Übernahme im Zielraum warten, älteste zuerst
        public List<Lagerbewegung> Unterwegs { get; set; } = new List<Lagerbewegung>();

        // Räume, für die die angemeldete Person verantwortlich ist (leer = keiner); nur von dort aus kann sie eine Ausleihe anlegen
        public List<string> EigeneRaumIDs { get; set; } = new List<string>();

        // Alle Buchungen des Gegenstands als Verlauf, neueste zuerst
        public List<Aktivitaet> Verlauf { get; set; } = new List<Aktivitaet>();
    }
}
