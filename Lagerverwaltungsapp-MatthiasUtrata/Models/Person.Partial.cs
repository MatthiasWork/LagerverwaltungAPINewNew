namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse, damit sie beim erneuten Generieren nicht überschrieben wird.
public partial class Person
{
    /// <summary>
    /// Die Mindestlänge eines neuen Passworts. Beim Anmelden gilt sie nicht, damit sich auch Benutzer mit einem kürzeren Passwort anmelden können.
    /// </summary>
    public const int PasswortMindestLaenge = 5;

    /// <summary>
    /// Der Vor- und Nachname zusammen, z. B. "Lena Brandt".
    /// </summary>
    public string VollerName => $"{Vorname} {Nachname}";

    /// <summary>
    /// Die Anfangsbuchstaben von Vor- und Nachname.
    /// </summary>
    public string Initialen => InitialenAus(Vorname, Nachname);

    /// <summary>
    /// Methode, die die Anfangsbuchstaben von Vor- und Nachname liefert, z. B. "LB" für Lena Brandt.
    /// </summary>
    /// <param name="vorname">Der Vorname, darf leer oder null sein.</param>
    /// <param name="nachname">Der Nachname, darf leer oder null sein.</param>
    /// <returns>Die Anfangsbuchstaben; bei einem leeren Namen fehlt sein Buchstabe.</returns>
    public static string InitialenAus(string? vorname, string? nachname)
    {
        return ErsterBuchstabe(vorname) + ErsterBuchstabe(nachname);
    }

    /// <summary>
    /// Methode, die den ersten Buchstaben eines Textes liefert. Null ist möglich, weil die Validierung auch bei einem fehlenden Namen liest.
    /// </summary>
    /// <param name="text">Der Text, darf leer oder null sein.</param>
    /// <returns>Der erste Buchstabe oder ein leerer Text.</returns>
    private static string ErsterBuchstabe(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }
        return text[..1];
    }
}
