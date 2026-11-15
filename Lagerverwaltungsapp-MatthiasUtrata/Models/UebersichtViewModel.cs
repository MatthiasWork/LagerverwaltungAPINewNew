namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebersichtViewModel
    {
        // Die Stückzahl, die gerade in einem Raum liegt.
        public int Verfuegbar { get; set; }

        // Die Stückzahl insgesamt, auch was gerade unterwegs ist.
        public int Gesamt { get; set; }

        // Die Anzahl der Lagerbewegungen, die noch auf die Übernahme warten.
        public int OffeneFreigaben { get; set; }

        // Die letzten Buchungen, neueste zuerst.
        public List<Aktivitaet> Aktivitaeten { get; set; } = new List<Aktivitaet>();

        // Die verfügbaren Stück je Kategorie, die größte zuerst.
        public List<KategorieStatistik> BestandJeKategorie { get; set; } = new List<KategorieStatistik>();

        // Die verfügbaren Stück je Raum, der größte zuerst.
        public List<RaumStatistik> BestandJeRaum { get; set; } = new List<RaumStatistik>();
    }

    // Ein Eintrag der Statistik nach Kategorie.
    public class KategorieStatistik
    {
        public int KategorieID { get; set; }

        public string Name { get; set; } = null!;

        // Die Summe der Mengen im Raumbestand ohne das, was unterwegs ist.
        public int Stueck { get; set; }
    }

    // Ein Eintrag der Statistik nach Raum.
    public class RaumStatistik
    {
        public string RaumID { get; set; } = null!;

        // Der Name der Raumart.
        public string Raumart { get; set; } = null!;

        // Die Summe der Mengen im Raumbestand des Raums.
        public int Stueck { get; set; }
    }

    // Ein Eintrag in der Liste der Aktivitäten.
    public class Aktivitaet
    {
        public DateTime Zeitpunkt { get; set; }

        public string Text { get; set; } = null!;
    }
}
