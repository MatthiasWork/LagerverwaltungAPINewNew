using Microsoft.Extensions.Caching.Memory;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Diese Klasse schützt vor dem Erraten von Passwörtern. Nach 5 Fehlschlägen ist ein Benutzername 1 Minute gesperrt,
    /// mit jedem weiteren Fehlschlag eine Minute länger.
    /// </summary>
    public class LoginSperreService
    {
        /// <summary>
        /// Die Anzahl der Fehlschläge, ab der die Sperre beginnt.
        /// </summary>
        public const int ErlaubteFehlschlaege = 5;

        // So lange wird ein Zähler nach der Sperre oder dem letzten Fehlschlag gemerkt, danach beginnt er von vorn.
        private static readonly TimeSpan VergessenNach = TimeSpan.FromHours(1);

        private readonly IMemoryCache _cache;
        private readonly object _schloss = new object();

        /// <summary>
        /// Konstruktor, der den Speicher für die Fehlschläge übernimmt.
        /// </summary>
        /// <param name="cache">Der Speicher, in dem die Fehlschläge je Benutzername liegen.</param>
        public LoginSperreService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Methode, die ermittelt, wie lange ein Benutzername noch gesperrt ist.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Die Restzeit der Sperre oder null, wenn der Benutzername nicht gesperrt ist.</returns>
        public TimeSpan? RestzeitDerSperre(string username)
        {
            lock (_schloss)
            {
                if (_cache.TryGetValue(Schluessel(username), out Stand? stand) && stand != null && stand.GesperrtBis > DateTime.UtcNow)
                {
                    return stand.GesperrtBis - DateTime.UtcNow;
                }
                return null;
            }
        }

        /// <summary>
        /// Methode, die einen Anmeldeversuch beginnt und ihn sofort als Fehlschlag zählt. Ein gesperrter Benutzername wird nicht gezählt.
        /// Das Prüfen und Zählen geschieht zusammen, damit gleichzeitige Versuche nicht alle an der Sperre vorbeikommen.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Die Restzeit der Sperre oder null, wenn das Passwort geprüft werden darf.</returns>
        public TimeSpan? VersuchBeginnen(string username)
        {
            lock (_schloss)
            {
                var schluessel = Schluessel(username);
                if (!_cache.TryGetValue(schluessel, out Stand? stand) || stand == null)
                {
                    stand = new Stand();
                }

                var jetzt = DateTime.UtcNow;
                if (stand.GesperrtBis > jetzt)
                {
                    return stand.GesperrtBis - jetzt;
                }

                stand.Fehlschlaege++;
                if (stand.Fehlschlaege >= ErlaubteFehlschlaege)
                {
                    stand.GesperrtBis = jetzt + TimeSpan.FromMinutes(stand.Fehlschlaege - ErlaubteFehlschlaege + 1);
                }

                var gemerktBis = (stand.GesperrtBis > jetzt ? stand.GesperrtBis : jetzt) + VergessenNach;
                _cache.Set(schluessel, stand, new DateTimeOffset(gemerktBis));
                return null;
            }
        }

        /// <summary>
        /// Methode, die nach einer erfolgreichen Anmeldung die Fehlschläge des Benutzernamens zurücksetzt.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        public void ErfolgMelden(string username)
        {
            lock (_schloss)
            {
                _cache.Remove(Schluessel(username));
            }
        }

        /// <summary>
        /// Methode, die aus dem Benutzernamen den Speicherschlüssel bildet, ohne Groß-/Kleinschreibung und Leerzeichen am Ende.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <returns>Der Schlüssel für den Speicher.</returns>
        private static string Schluessel(string username)
        {
            return "loginsperre:" + username.Trim().ToLowerInvariant();
        }

        private class Stand
        {
            public int Fehlschlaege { get; set; }

            public DateTime GesperrtBis { get; set; }
        }
    }
}
