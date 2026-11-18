namespace Lagerverwaltung.Api.DTOs
{
    public class TokenLesenDTO
    {
        public int PersonID { get; set; }

        public string Username { get; set; } = null!;

        public string Rolle { get; set; } = null!;

        public bool IstAdmin { get; set; }

        public DateTime LäuftAbAm { get; set; }
    }
}
