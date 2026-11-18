using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Raum
{
    [MaxLength(8)]
    [Unicode(false)]
    public string ID { get; set; } = null!;

    public int? PersonID { get; set; }

    public int RaumartID { get; set; }
}
