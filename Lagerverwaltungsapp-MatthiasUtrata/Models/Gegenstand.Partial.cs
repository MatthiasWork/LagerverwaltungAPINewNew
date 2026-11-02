namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Gegenstand, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Gegenstand
{
    /// <summary>
    /// So viele Stück kann es von einem Gegenstand höchstens geben, in allen Räumen und unterwegs zusammen. Mehr lassen Wareneingang
    /// und Korrektur nicht zu (Verleihen und Holen ändern die Summe nicht). Sonst könnten Mengen und Summen über den Bereich von int
    /// hinauswachsen und z. B. negativ werden.
    /// </summary>
    public const int HoechstBestand = 100000;
}
