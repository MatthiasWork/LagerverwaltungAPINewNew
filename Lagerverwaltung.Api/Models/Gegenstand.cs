namespace Lagerverwaltung.Api.Models;

public class Gegenstand
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string? Seriennummer { get; set; }

    public int KategorieID { get; set; }

    public int HerstellerID { get; set; }
}
