using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Rolle
{
    public int ID { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = null!;

    public bool Admin { get; set; }
}
