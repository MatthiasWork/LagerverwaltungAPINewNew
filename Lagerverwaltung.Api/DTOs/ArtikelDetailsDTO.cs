namespace Lagerverwaltung.Api.DTOs
{
    public class ArtikelDetailsDTO
    {
        public GegenstandLesenIDAlleDTO Artikel { get; set; } = null!;

        public List<RaumbestandByArtikelLesenDTO> Bestaende { get; set; } = new List<RaumbestandByArtikelLesenDTO>();
    }
}
