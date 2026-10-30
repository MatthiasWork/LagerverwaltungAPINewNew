using Microsoft.Extensions.Caching.Memory;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Schutz vor dem Erraten von Passwörtern: Nach 5 fehlgeschlagenen Anmeldungen mit demselben Benutzernamen ist dieser für
    /// 1 Minute gesperrt, mit jedem weiteren Fehlschlag um eine Minute länger (2, 3, 4 ... Minuten). Solange die Sperre gilt, wird
    /// das Passwort nicht geprüft, auch nicht das richtige. Eine erfolgreiche Anmeldung setzt den Zähler zurück. Benutzernamen,
    /// die es nicht gibt, werden genauso gezählt, damit man an der Sperre nicht erkennt, welche Benutzer es gibt.
    /// Der Stand liegt im Speicher der Anwendung und ist nach einem Neustart weg.
    /// </summary>
    public class LoginSperreService
    {
        /// <summary>
        /// Anzahl der Fehlschläge, ab der die Sperre beginnt (der 5. Fehlschlag sperrt).
        /// </summary>
        public const int ErlaubteFehlschlaege = 5;

        // Wie lange ein Zähler nach dem Ende der Sperre (bzw. nach dem letzten Fehlschlag) gemerkt wird. Danach beginnt es von vorn
        private static readonly TimeSpan VergessenNach = TimeSpan.FromHours(1);

        private readonly IMemoryCache _cache;
        private readonly object _schloss = new object();

        /// <summary>
        /// Konstruktor für den LoginSperreService.
        /// </summary>
        /// <param name="cache">Der Speicher, in dem die Fehlschläge je Benutzername liegen</param>
        public LoginSperreService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Methode, die ermittelt, wie lange ein Benutzername noch gesperrt ist.
        /// </summary>
        /// <param name="username">Der Benutzername aus dem Anmeldeformular</param>
        /// <returns>Die Restzeit der Sperre oder null, wenn der Benutzername nicht gesperrt ist</returns>
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
        /// Methode, die einen fehlgeschlagenen Anmeldeversuch zählt. Ab dem 5. Fehlschlag beginnt die Sperre, mit jedem weiteren
        /// Fehlschlag ist sie eine Minute länger als zuvor.
        /// </summary>
        /// <param name="username">Der Benutzername aus dem Anmeldeformular</param>
        /// <returns>Die Dauer der Sperre, die dieser Fehlschlag ausgelöst hat, oder null, wenn noch nicht gesperrt wird</returns>
        public TimeSpan? FehlschlagMelden(string username)
        {
            lock (_schloss)
            {
                var schluessel = Schluessel(username);
                if (!_cache.TryGetValue(schluessel, out Stand? stand) || stand == null)
                {
                    stand = new Stand();
                }

                stand.Fehlschlaege++;

                TimeSpan? sperre = null;
                var jetzt = DateTime.UtcNow;
                if (stand.Fehlschlaege >= ErlaubteFehlschlaege)
                {
                    sperre = TimeSpan.FromMinutes(stand.Fehlschlaege - ErlaubteFehlschlaege + 1);
                    stand.GesperrtBis = jetzt + sperre.Value;
                }

                var gemerktBis = jetzt + (sperre ?? TimeSpan.Zero) + VergessenNach;
                _cache.Set(schluessel, stand, new DateTimeOffset(gemerktBis));
                return sperre;
            }
        }

        /// <summary>
        /// Methode, die nach einer erfolgreichen Anmeldung die Fehlschläge des Benutzernamens vergisst.
        /// </summary>
        /// <param name="username">Der Benutzername aus dem Anmeldeformular</param>
        public void ErfolgMelden(string username)
        {
            lock (_schloss)
            {
                _cache.Remove(Schluessel(username));
            }
        }

        /// <summary>
        /// Methode, die den Benutzernamen so schreibt, wie ihn die Datenbank beim Vergleichen versteht: ohne Groß-/Kleinschreibung
        /// und ohne Leerzeichen am Ende ("admin", "Admin" und "admin " sind derselbe Benutzer und teilen sich einen Zähler).
        /// </summary>
        /// <param name="username">Der Benutzername aus dem Anmeldeformular</param>
        /// <returns>Der Schlüssel für den Speicher</returns>
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
