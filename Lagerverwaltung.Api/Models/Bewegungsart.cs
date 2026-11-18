using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Bewegungsart
{
    public int ID { get; set; }

    [MaxLength(20)]
    public string Name { get; set; } = null!;
}
