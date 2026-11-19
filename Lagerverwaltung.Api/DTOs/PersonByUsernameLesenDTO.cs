namespace Lagerverwaltung.Api.DTOs
{
    public class PersonByUsernameLesenDTO
    {
        public int ID { get; set; }

        public string Username { get; set; } = null!;

        public string PasswortHash { get; set; } = null!;
    }
}
