namespace Lagerverwaltung.Api.Models;

public class Raum
{
    public string ID { get; set; } = null!;

    public int? PersonID { get; set; }

    public int RaumartID { get; set; }
}
