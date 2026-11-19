namespace Lagerverwaltung.Api.DTOs
{
    public class ArtikelDetailsDTO
    {
        public ArtikelLesenDTO Artikel { get; set; } = null!;

        public List<RaumbestandByArtikelLesenDTO> Bestaende { get; set; } = new List<RaumbestandByArtikelLesenDTO>();
    }
}
