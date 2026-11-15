namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse, damit sie beim erneuten Generieren nicht überschrieben wird.
public partial class Raum
{
    /// <summary>
    /// Die Raumnummer des Umbuchungslagers, in das zurückgegebene Geräte kommen.
    /// Sie wird statt der Raumart verwendet, weil sich die Raumnummer nicht ändern lässt, der Name der Raumart aber schon.
    /// </summary>
    public const string Umbuchungslager = "UL";

    /// <summary>
    /// Methode, die prüft, ob eine Raumnummer die des Umbuchungslagers ist.
    /// </summary>
    /// <param name="raumID">Die Raumnummer, darf null sein.</param>
    /// <returns>True, wenn es das Umbuchungslager ist.</returns>
    public static bool IstUmbuchungslager(string? raumID)
    {
        return string.Equals(raumID, Umbuchungslager, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Methode, die prüft, ob eine Person für diesen Raum buchen und freigeben darf. Bei einem Lager darf das auch jeder Admin.
    /// Die Raumart muss geladen sein.
    /// </summary>
    /// <param name="personID">Die ID der Person.</param>
    /// <param name="istAdmin">True, wenn die Person Admin ist.</param>
    /// <returns>True, wenn die Person buchen darf.</returns>
    public bool DarfBuchen(int personID, bool istAdmin)
    {
        return PersonID == personID || (istAdmin && Raumart.IstLager);
    }
}
