using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lagerverwaltung.Api.Models;

public class Lagerbewegung
{
    public int ID { get; set; }

    public int Menge { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ErstelltAm { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? BestaetigtAm { get; set; }

    public int BewegungsartID { get; set; }

    [MaxLength(8)]
    [Unicode(false)]
    public string VonRaumID { get; set; } = null!;

    [MaxLength(8)]
    [Unicode(false)]
    public string NachRaumID { get; set; } = null!;

    public int GegenstandID { get; set; }

    public int PersonID { get; set; }
}
