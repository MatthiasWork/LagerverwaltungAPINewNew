namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebernahmeViewModel
    {
        // Die Räume, für die die angemeldete Person verantwortlich ist (mit Raumart, leer = keiner)
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Für einen Admin die Lager, für die er zusätzlich freigibt (Raum.DarfBuchen), sonst leer
        public List<Raum> Lager { get; set; } = new List<Raum>();

        // Offene Lagerbewegungen, die die Person freigeben muss (Ausleihen in den Raum, aus ihm geholte Geräte, bei einem Admin auch die der Lager)
        public List<Lagerbewegung> Offen { get; set; } = new List<Lagerbewegung>();

        // Offene Lagerbewegungen, die ein Admin aus einem Lager an einen anderen Raum (kein Lager) angefragt hat und hier zurückziehen kann
        // (die aus dem eigenen Raum stehen unter "Mein Raum", die an ein Lager oben unter Offen)
        public List<Lagerbewegung> Angefragt { get; set; } = new List<Lagerbewegung>();

        // Die zuletzt erledigten (bestätigten oder stornierten) Lagerbewegungen, die die Person freigeben musste
        public List<Lagerbewegung> Erledigt { get; set; } = new List<Lagerbewegung>();

        // Ob es etwas gibt, wofür die Person freigibt: einen eigenen Raum oder als Admin ein Lager
        public bool GibtFrei => Raeume.Count > 0 || Lager.Count > 0;
    }
}
