namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel des Gerätedetails.
    public class GegenstandDetailsViewModel
    {
        // Der Gegenstand mit Standorten, Stückzahl und Status.
        public KatalogEintrag Eintrag { get; set; } = null!;

        // Die Lagerbewegungen, die noch auf die Übernahme warten, älteste zuerst.
        public List<Lagerbewegung> Unterwegs { get; set; } = new List<Lagerbewegung>();

        // Die IDs der Räume, für die die angemeldete Person verantwortlich ist.
        public List<string> EigeneRaumIDs { get; set; } = new List<string>();

        // Alle Buchungen des Gegenstands, neueste zuerst.
        public List<Aktivitaet> Verlauf { get; set; } = new List<Aktivitaet>();
    }
}
