using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Gegenstand
{
    public int ID { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    public string? Seriennummer { get; set; }

    public int KategorieID { get; set; }

    public int HerstellerID { get; set; }
}
