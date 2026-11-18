namespace Lagerverwaltung.Api.DTOs
{
    public class LoginAntwortDTO
    {
        public string Token { get; set; } = null!;

        public DateTime LaeuftAbAm { get; set; }

        public PersonLesenIDDTO Benutzer { get; set; } = null!;
    }
}
