using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MeinRaumViewModel
    {
        // Der angezeigte Raum, für den die angemeldete Person verantwortlich ist.
        public Raum Raum { get; set; } = null!;

        // Alle Räume der Person.
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Der gefilterte Bestand des Raums.
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Die Anzahl aller Bestandszeilen ohne Filter.
        public int AnzahlGesamt { get; set; }

        // Die Summe aller Mengen im Raum.
        public int StueckGesamt { get; set; }

        // Die für den Raum angefragten Lagerbewegungen, die noch nicht freigegeben sind und die die Person zurückziehen kann.
        public List<Lagerbewegung> Unterwegs { get; set; } = new List<Lagerbewegung>();

        // Die Anzahl der offenen Lagerbewegungen, die die Person freigeben muss.
        public int OffeneUebernahmen { get; set; }

        // Die aktuellen Filterwerte.
        public string? Suche { get; set; }

        public int? KategorieID { get; set; }

        public SelectList Kategorien { get; set; } = null!;
    }
}
