namespace Lagerverwaltung.Api.DTOs
{
    public class ProfilDTO
    {
        public PersonLesenDTO Benutzer { get; set; } = null!;

        // Die Räume, für die die Person zuständig ist, mit Positionen und Stück
        public List<RaumLesenDTO> Raeume { get; set; } = new List<RaumLesenDTO>();

        // Lagerbewegungen, die die Person angelegt hat und die noch nicht bestätigt oder storniert sind
        public List<LagerbewegungLesenDTO> OffeneBewegungen { get; set; } = new List<LagerbewegungLesenDTO>();

        // Die neuesten Lagerbewegungen der Person und ihrer Räume
        public List<LagerbewegungLesenDTO> LetzteBewegungen { get; set; } = new List<LagerbewegungLesenDTO>();
    }
}
