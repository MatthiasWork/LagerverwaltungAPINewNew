namespace Lagerverwaltung.Api.DTOs
{
    public class PersonLesenIDDTO
    {
        public int ID { get; set; }

        public string Vorname { get; set; } = null!;

        public string Nachname { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        public int RolleID { get; set; }

        public string Rolle { get; set; } = null!;

        public bool IstAdmin { get; set; }

        public string? Raeume { get; set; }
    }
}
