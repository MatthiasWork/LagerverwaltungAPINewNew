namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Auswahl des eigenen Raums (_RaumWechsel), wenn eine Person für mehrere Räume verantwortlich ist.
    // Jeder Raum ist ein Link auf dieselbe Seite mit diesem Raum
    public class RaumWechselViewModel
    {
        // Die Räume der angemeldeten Person (mit Raumart)
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Der gerade angezeigte Raum
        public string AktivID { get; set; } = null!;

        // Controller und Action der Seite, auf die die Links führen
        public string Controller { get; set; } = null!;

        public string Action { get; set; } = "Index";

        // Name des Parameters, in dem die Raumnummer übergeben wird (z. B. "id" oder "vonRaumID")
        public string Parameter { get; set; } = "id";
    }
}
