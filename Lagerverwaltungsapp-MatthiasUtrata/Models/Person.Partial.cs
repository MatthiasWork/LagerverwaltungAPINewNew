namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Person, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Person
{
    /// <summary>
    /// Wie viele Zeichen ein neues Passwort mindestens haben muss (beim Registrieren, Anlegen und Ändern eines Benutzers).
    /// Beim Anmelden gilt sie nicht, damit sich auch Benutzer mit einem älteren, kürzeren Passwort anmelden können.
    /// </summary>
    public const int PasswortMindestLaenge = 5;

    /// <summary>
    /// Vor- und Nachname, z. B. "Lena Brandt". Nur für geladene Personen, in Abfragen Vorname und Nachname verwenden.
    /// </summary>
    public string VollerName => $"{Vorname} {Nachname}";

    /// <summary>
    /// Anfangsbuchstaben von Vor- und Nachname, z. B. für den runden Avatar.
    /// </summary>
    public string Initialen => InitialenAus(Vorname, Nachname);

    /// <summary>
    /// Methode, die die Anfangsbuchstaben von Vor- und Nachname liefert, z. B. "LB" für Lena Brandt.
    /// Auch für das Menü, das nur die Namen aus dem Login hat und keine Person.
    /// </summary>
    /// <param name="vorname">Der Vorname, darf leer oder null sein</param>
    /// <param name="nachname">Der Nachname, darf leer oder null sein</param>
    /// <returns>Die Anfangsbuchstaben; bei einem leeren Namen fehlt sein Buchstabe</returns>
    public static string InitialenAus(string? vorname, string? nachname)
    {
        return ErsterBuchstabe(vorname) + ErsterBuchstabe(nachname);
    }

    /// <summary>
    /// Methode, die den ersten Buchstaben eines Textes liefert. Null ist möglich, weil die Eingabevalidierung die Eigenschaften
    /// einer Person auch dann liest, wenn im Formular ein Name fehlt.
    /// </summary>
    /// <param name="text">Der Text, darf leer oder null sein</param>
    /// <returns>Der erste Buchstabe oder ein leerer Text, wenn der Text leer ist</returns>
    private static string ErsterBuchstabe(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }
        return text[..1];
    }
}
