namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse, damit sie beim erneuten Generieren nicht überschrieben wird.
public partial class Bewegungsart
{
    /// <summary>
    /// Der Name der Bewegungsart, die bei einer neuen Ausleihe vorausgewählt wird.
    /// </summary>
    public const string Ausgabe = "Ausgabe";

    /// <summary>
    /// Der Name der Bewegungsart, die beim Wareneingang vorausgewählt wird.
    /// </summary>
    public const string Wareneingang = "Wareneingang";

    /// <summary>
    /// Der Name der Bewegungsart für abgelehnte und zurückgezogene Lagerbewegungen.
    /// Sie wird nur vom LagerService vergeben und kann weder gewählt noch umbenannt oder gelöscht werden.
    /// </summary>
    public const string Storniert = "Storniert";

    /// <summary>
    /// Der Name der Bewegungsart für Korrekturbuchungen im Bestand eines Raums.
    /// Sie wird nur vom LagerService vergeben und kann weder gewählt noch umbenannt oder gelöscht werden.
    /// </summary>
    public const string Korrektur = "Korrektur";

    /// <summary>
    /// Der Name der Bewegungsart für Rückgaben an das Umbuchungslager.
    /// Sie wird nur vom LagerService vergeben und kann weder gewählt noch umbenannt oder gelöscht werden.
    /// </summary>
    public const string Rueckgabe = "Rückgabe";

    /// <summary>
    /// Methode, die beschreibt, für welche Lagerbewegungen eine nur vom LagerService vergebene Bewegungsart gilt.
    /// </summary>
    /// <param name="name">Der Name der Bewegungsart.</param>
    /// <returns>Die Beschreibung oder null, wenn die Bewegungsart frei gewählt werden kann.</returns>
    public static string? NurVergebenFuer(string? name)
    {
        if (string.Equals(name?.Trim(), Storniert, StringComparison.OrdinalIgnoreCase))
        {
            return "abgelehnte und zurückgezogene Ausleihen";
        }
        if (string.Equals(name?.Trim(), Korrektur, StringComparison.OrdinalIgnoreCase))
        {
            return "Korrekturbuchungen im Lagerbestand";
        }
        if (string.Equals(name?.Trim(), Rueckgabe, StringComparison.OrdinalIgnoreCase))
        {
            return "Rückgaben an das Umbuchungslager";
        }
        return null;
    }
}
