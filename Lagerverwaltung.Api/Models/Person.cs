using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltung.Api.Models;

public class Person
{
    public int ID { get; set; }

    [MaxLength(250)]
    public string Vorname { get; set; } = null!;

    [MaxLength(250)]
    public string Nachname { get; set; } = null!;

    [MaxLength(20)]
    public string Username { get; set; } = null!;

    [Unicode(false)]
    public string Password { get; set; } = null!;

    [MaxLength(100)]
    public string Email { get; set; } = null!;

    public int RolleID { get; set; }
}
