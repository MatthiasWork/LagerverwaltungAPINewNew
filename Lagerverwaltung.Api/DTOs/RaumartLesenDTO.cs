namespace Lagerverwaltung.Api.DTOs
{
    public class RaumartLesenDTO
    {
        public int ID { get; set; }

        public string Name { get; set; } = null!;

        public bool IstLager { get; set; }
    }
}
