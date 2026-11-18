using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Raumart
{
    public int ID { get; set; }

    [MaxLength(20)]
    public string Name { get; set; } = null!;
}
