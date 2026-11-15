namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Das ViewModel des Profils.
    public class ProfilViewModel
    {
        // Die Person mit Rolle und Räumen.
        public Person Person { get; set; } = null!;

        // Gibt an, ob die angemeldete Person ihr eigenes Profil ansieht.
        public bool IstEigenesProfil { get; set; }

        // Die Stückzahl im Bestand je Raum der Person (Schlüssel ist die RaumID).
        public Dictionary<string, int> StueckJeRaum { get; set; } = new Dictionary<string, int>();

        // Die Ausleihen der Person, die noch auf die Übernahme warten.
        public List<Lagerbewegung> OffeneAusleihen { get; set; } = new List<Lagerbewegung>();

        // Die letzten Buchungen und Übernahmen, neueste zuerst.
        public List<Aktivitaet> Verlauf { get; set; } = new List<Aktivitaet>();
    }
}
