namespace Lagerverwaltung.Api.DTOs
{
    public class RolleLesenDTO
    {
        public int ID { get; set; }

        public string Name { get; set; } = null!;

        public bool Admin { get; set; }
    }
}
