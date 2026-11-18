namespace Lagerverwaltung.Api.DTOs
{
    public class RaumbestandByRaumLesenDTO
    {
        public int GegenstandID { get; set; }

        public string Name { get; set; } = null!;

        public string? Seriennummer { get; set; }

        public int KategorieID { get; set; }

        public string Kategorie { get; set; } = null!;

        public string Hersteller { get; set; } = null!;

        public int Menge { get; set; }
    }
}
