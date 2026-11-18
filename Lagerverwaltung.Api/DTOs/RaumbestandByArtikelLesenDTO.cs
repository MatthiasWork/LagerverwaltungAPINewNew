namespace Lagerverwaltung.Api.DTOs
{
    public class RaumbestandByArtikelLesenDTO
    {
        public string RaumID { get; set; } = null!;

        public string Raumart { get; set; } = null!;

        public int? VerantwortlichID { get; set; }

        public string? Verantwortlich { get; set; }

        public int Menge { get; set; }
    }
}
