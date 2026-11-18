using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Kategorie
{
    public int ID { get; set; }

    [MaxLength(30)]
    public string Name { get; set; } = null!;
}
