using System.ComponentModel.DataAnnotations.Schema;

namespace Lagerverwaltung.Api.Models;

public class Token
{
    public int ID { get; set; }

    public int PersonID { get; set; }

    [Column(TypeName = "binary(32)")]
    public byte[] TokenHash { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ErstelltAm { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LäuftAbAm { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AbgelaufenAm { get; set; }
}
