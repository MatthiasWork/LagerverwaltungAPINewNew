namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Raum, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Raum
{
    /// <summary>
    /// Raumnummer des Umbuchungslagers. Zurückgegebene Geräte kommen immer dorthin (Rueckgabe/Index), die dort verantwortliche
    /// Person bestätigt die Rückgabe und lagert sie danach z. B. ins Hauptlager ein. Über die Raumnummer und nicht über die
    /// Raumart, da sich die Raumnummer nicht ändern lässt, der Name der Raumart aber schon. Angelegt wird der Raum mit Testdaten.sql.
    /// </summary>
    public const string Umbuchungslager = "UL";

    /// <summary>
    /// Methode, die überprüft, ob eine Raumnummer die des Umbuchungslagers ist. Ohne Beachtung der Groß-/Kleinschreibung, wie SQL Server beim Suchen.
    /// </summary>
    /// <param name="raumID">Die Raumnummer oder null (z. B. wenn die Person keinen Raum hat)</param>
    /// <returns>Gibt True oder False zurück</returns>
    public static bool IstUmbuchungslager(string? raumID)
    {
        return string.Equals(raumID, Umbuchungslager, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Person für diesen Raum buchen und freigeben darf: die verantwortliche Person und, wenn der
    /// Raum ein Lager ist (Raumart.IstLager), auch jeder Admin. So verwalten Admins Hauptlager und Umbuchungslager, auch ohne eigenen Raum.
    /// Dafür muss die Raumart geladen sein.
    /// </summary>
    /// <param name="personID">Die ID der Person</param>
    /// <param name="istAdmin">True, wenn die Person eine Rolle mit Admin-Rechten hat</param>
    /// <returns>Gibt True oder False zurück</returns>
    public bool DarfBuchen(int personID, bool istAdmin)
    {
        return PersonID == personID || (istAdmin && Raumart.IstLager);
    }
}
