namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel für die Auswahl des eigenen Raums, wenn eine Person für mehrere Räume verantwortlich ist.
    public class RaumWechselViewModel
    {
        // Die Räume der angemeldeten Person.
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Der gerade angezeigte Raum.
        public string AktivID { get; set; } = null!;

        // Controller und Action der Seite, auf die die Links führen.
        public string Controller { get; set; } = null!;

        public string Action { get; set; } = "Index";

        // Der Name des Parameters, in dem die Raumnummer übergeben wird.
        public string Parameter { get; set; } = "id";
    }
}
