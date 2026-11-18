namespace Lagerverwaltung.Api.DTOs
{
    public class LagerbewegungLesenDTO
    {
        public int ID { get; set; }

        public int GegenstandID { get; set; }

        public string Gegenstand { get; set; } = null!;

        public string? Seriennummer { get; set; }

        public int Menge { get; set; }

        public string VonRaumID { get; set; } = null!;

        public string NachRaumID { get; set; } = null!;

        public int BewegungsartID { get; set; }

        public string Bewegungsart { get; set; } = null!;

        public string Status { get; set; } = null!;

        public DateTime ErstelltAm { get; set; }

        public DateTime? BestaetigtAm { get; set; }

        public int PersonID { get; set; }

        public string Person { get; set; } = null!;

        public bool DarfBestaetigen { get; set; }
    }
}
