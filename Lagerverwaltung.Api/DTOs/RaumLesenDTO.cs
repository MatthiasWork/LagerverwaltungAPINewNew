namespace Lagerverwaltung.Api.DTOs
{
    public class RaumLesenDTO
    {
        public string ID { get; set; } = null!;

        public int RaumartID { get; set; }

        public string Raumart { get; set; } = null!;

        public bool IstLager { get; set; }

        public int? PersonID { get; set; }

        public string? Verantwortlich { get; set; }

        public int Positionen { get; set; }

        public int Stueck { get; set; }
    }
}
