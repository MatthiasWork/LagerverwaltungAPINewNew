namespace Lagerverwaltung.Api.DTOs
{
    public class PersonLesenUsernameDTO
    {
        public int ID { get; set; }

        public string Username { get; set; } = null!;

        public string PasswortHash { get; set; } = null!;
    }
}
