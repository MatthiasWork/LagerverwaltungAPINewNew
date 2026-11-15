namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebernahmeViewModel
    {
        // Die Räume, für die die angemeldete Person verantwortlich ist.
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Die Lager, für die ein Admin zusätzlich freigibt; sonst leer.
        public List<Raum> Lager { get; set; } = new List<Raum>();

        // Die offenen Lagerbewegungen, die die Person freigeben muss.
        public List<Lagerbewegung> Offen { get; set; } = new List<Lagerbewegung>();

        // Die offenen Lagerbewegungen, die ein Admin aus einem Lager an einen anderen Raum angefragt hat und hier zurückziehen kann.
        public List<Lagerbewegung> Angefragt { get; set; } = new List<Lagerbewegung>();

        // Die zuletzt erledigten Lagerbewegungen, die die Person freigeben musste.
        public List<Lagerbewegung> Erledigt { get; set; } = new List<Lagerbewegung>();

        // Gibt an, ob die Person für einen Raum oder als Admin für ein Lager freigibt.
        public bool GibtFrei => Raeume.Count > 0 || Lager.Count > 0;
    }
}
