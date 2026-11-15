namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse, damit sie beim erneuten Generieren nicht überschrieben wird.
public partial class Gegenstand
{
    /// <summary>
    /// Die höchste Stückzahl eines Gegenstands in allen Räumen und unterwegs zusammen.
    /// Sonst könnten Summen über den Bereich von int hinauswachsen.
    /// </summary>
    public const int HoechstBestand = 100000;
}
