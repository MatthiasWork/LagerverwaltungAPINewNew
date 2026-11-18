namespace Lagerverwaltung.Api.DTOs
{
    public class GegenstandLesenIDAlleDTO
    {
        public int ID { get; set; }

        public string Name { get; set; } = null!;

        public string? Seriennummer { get; set; }

        public int KategorieID { get; set; }

        public string Kategorie { get; set; } = null!;

        public int HerstellerID { get; set; }

        public string Hersteller { get; set; } = null!;

        public int Bestand { get; set; }

        public int Unterwegs { get; set; }
    }
}
