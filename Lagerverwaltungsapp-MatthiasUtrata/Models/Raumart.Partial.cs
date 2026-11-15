namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse, damit sie beim erneuten Generieren nicht überschrieben wird.
public partial class Raumart
{
    /// <summary>
    /// Der Wortteil, an dem ein Lager erkannt wird: Der Name der Raumart enthält ihn, egal ob groß oder klein geschrieben.
    /// </summary>
    public const string Lager = "lager";

    /// <summary>
    /// Gibt an, ob Räume mit dieser Raumart Lager sind.
    /// </summary>
    public bool IstLager => Name.Contains(Lager, StringComparison.OrdinalIgnoreCase);
}
