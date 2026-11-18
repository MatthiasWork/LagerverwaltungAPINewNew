using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Raumbestand
{
    public int GegenstandID { get; set; }

    [MaxLength(8)]
    [Unicode(false)]
    public string RaumID { get; set; } = null!;

    public int Menge { get; set; }
}
