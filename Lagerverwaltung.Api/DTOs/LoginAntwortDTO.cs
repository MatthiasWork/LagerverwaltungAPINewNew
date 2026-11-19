namespace Lagerverwaltung.Api.DTOs
{
    public class LoginAntwortDTO
    {
        public string Token { get; set; } = null!;

        public DateTime LäuftAbAm { get; set; }

        public PersonLesenDTO Benutzer { get; set; } = null!;
    }
}
