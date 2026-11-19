using System.Data;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    public class LagerService
    {
        private readonly LagerverwaltungContext _context;
        private readonly ILogger<LagerService> _logger;

        /// <summary>
        /// Konstruktor, der den Datenbankkontext und den Logger übernimmt.
        /// </summary>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="logger">Der Logger für fehlgeschlagene Buchungen.</param>
        public LagerService(LagerverwaltungContext context, ILogger<LagerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Methode, die prüft, ob eine Person Admin ist. Sie fragt die Datenbank, damit eine geänderte Rolle sofort gilt.
        /// </summary>
        /// <param name="personID">Die ID der Person.</param>
        /// <returns>True, wenn die Person Admin ist.</returns>
        private async Task<bool> IstAdminAsync(int personID)
        {
            return await _context.Person.AnyAsync(p => p.ID == personID && p.Rolle.Admin);
        }

        /// <summary>
        /// Methode, die alle Lager ermittelt.
        /// </summary>
        /// <returns>Die Lager nach Raumnummer sortiert.</returns>
        public async Task<List<Raum>> LagerAsync()
        {
            return await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.Raumart.Name.ToLower().Contains(Raumart.Lager))
                .OrderBy(r => r.ID)
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die die Räume ermittelt, für die eine Person verantwortlich ist.
        /// </summary>
        /// <param name="personID">Die ID der Person oder null, wenn niemand angemeldet ist.</param>
        /// <returns>Die Räume nach Raumnummer sortiert, oder eine leere Liste.</returns>
        public async Task<List<Raum>> RaeumeDerPersonAsync(int? personID)
        {
            if (personID == null)
            {
                return new List<Raum>();
            }

            return await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.PersonID == personID)
                .OrderBy(r => r.ID)
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die aus den Räumen einer Person den gewünschten Raum heraussucht. Die Groß-/Kleinschreibung spielt dabei keine Rolle.
        /// </summary>
        /// <param name="raeume">Die Räume der Person.</param>
        /// <param name="raumID">Die gewünschte Raumnummer.</param>
        /// <returns>Der Raum oder null, wenn die Person dafür nicht verantwortlich ist.</returns>
        public static Raum? RaumAus(IEnumerable<Raum> raeume, string? raumID)
        {
            return raeume.FirstOrDefault(r => string.Equals(r.ID, raumID?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Methode, die die Bewegungsarten liefert, die man beim Buchen auswählen kann.
        /// "Storniert", "Korrektur" und "Rückgabe" fehlen, weil nur der LagerService sie vergibt.
        /// </summary>
        /// <returns>Die Bewegungsarten nach Namen sortiert.</returns>
        public async Task<List<Bewegungsart>> WaehlbareBewegungsartenAsync()
        {
            return await _context.Bewegungsart
                .Where(b => b.Name != Bewegungsart.Storniert && b.Name != Bewegungsart.Korrektur && b.Name != Bewegungsart.Rueckgabe)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die einen Wareneingang bucht: Ein Gegenstand kommt von außen in einen Raum.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands.</param>
        /// <param name="menge">Die Menge.</param>
        /// <param name="raumID">Die ID des Raums, in den der Gegenstand eingeht.</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn gebucht wurde.</returns>
        public async Task<string?> WareneingangAsync(int gegenstandID, int menge, string raumID, int bewegungsartID, int personID)
        {
            return await BewegungAnlegenAsync(gegenstandID, menge, raumID, raumID, bewegungsartID, personID);
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung anlegt und die Menge sofort im Von-Raum abbucht.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands.</param>
        /// <param name="menge">Die Menge.</param>
        /// <param name="vonRaumID">Die ID des Von-Raums.</param>
        /// <param name="nachRaumID">Die ID des Nach-Raums.</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung angelegt wurde.</returns>
        public async Task<string?> BewegungAnlegenAsync(int gegenstandID, int menge, string vonRaumID, string nachRaumID, int bewegungsartID, int personID)
        {
            return await BuchenAsync(() => BewegungBuchenAsync(gegenstandID, menge, vonRaumID, nachRaumID, bewegungsartID, personID));
        }

        /// <summary>
        /// Methode, die mehrere Gegenstände von einem Raum in einen anderen bucht, zum Beispiel bei einer Ausleihe.
        /// </summary>
        /// <param name="positionen">Die Gegenstände mit der Menge (Schlüssel = GegenstandID, Wert = Menge).</param>
        /// <param name="vonRaumID">Die ID des Von-Raums.</param>
        /// <param name="nachRaumID">Die ID des Nach-Raums.</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Gegenstände gebucht wurden.</returns>
        public async Task<string?> AusborgenAsync(IReadOnlyDictionary<int, int> positionen, string vonRaumID, string nachRaumID, int bewegungsartID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                if (positionen.Count == 0)
                {
                    return "Bitte mindestens einen Gegenstand auswählen.";
                }

                // Derselbe Raum wäre ein Wareneingang, dabei würde sich der Bestand verdoppeln.
                var vonRaum = await _context.Raum.FindAsync(vonRaumID);
                var nachRaum = await _context.Raum.FindAsync(nachRaumID);
                if (vonRaum != null && nachRaum != null && vonRaum.ID == nachRaum.ID)
                {
                    return $"Raum {vonRaum.ID} kann nicht an sich selbst verleihen. Bitte einen anderen Zielraum auswählen.";
                }

                foreach (var (gegenstandID, menge) in positionen)
                {
                    var fehler = await BewegungBuchenAsync(gegenstandID, menge, vonRaumID, nachRaumID, bewegungsartID, personID);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, die Gegenstände aus einem Raum an das Umbuchungslager zurückgibt. Sie sind unterwegs, bis die Rückgabe dort freigegeben wird.
        /// </summary>
        /// <param name="positionen">Die Gegenstände mit der Menge (Schlüssel = GegenstandID, Wert = Menge).</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem zurückgegeben wird.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Gegenstände gebucht wurden.</returns>
        public async Task<string?> ZurueckgebenAsync(IReadOnlyDictionary<int, int> positionen, string vonRaumID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                if (positionen.Count == 0)
                {
                    return "Bitte mindestens einen Gegenstand zum Zurückgeben auswählen.";
                }

                // Diese Bewegungsart kommt nur aus dem SQL-Skript Testdaten.sql.
                var rueckgabe = await _context.Bewegungsart.FirstOrDefaultAsync(b => b.Name == Bewegungsart.Rueckgabe);
                if (rueckgabe == null)
                {
                    return $"Die Bewegungsart \"{Bewegungsart.Rueckgabe}\" fehlt in der Datenbank. Sie wird mit dem SQL-Skript Testdaten.sql angelegt.";
                }

                var umbuchungslager = await _context.Raum.FindAsync(Raum.Umbuchungslager);
                if (umbuchungslager == null)
                {
                    return $"Das Umbuchungslager (Raum {Raum.Umbuchungslager}) gibt es nicht, daher kann nichts zurückgegeben werden. Ein Admin kann es in der Raumliste anlegen.";
                }

                // Das Umbuchungslager lagert Geräte über "Verleihen" weiter ein und gibt nicht an sich selbst zurück.
                var vonRaum = await _context.Raum.FindAsync(vonRaumID);
                if (vonRaum != null && vonRaum.ID == umbuchungslager.ID)
                {
                    return "Das Umbuchungslager kann nicht an sich selbst zurückgeben.";
                }

                foreach (var (gegenstandID, menge) in positionen)
                {
                    var fehler = await BewegungBuchenAsync(gegenstandID, menge, vonRaumID, umbuchungslager.ID, rueckgabe.ID, personID, festeBewegungsart: true);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung bestätigt.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung bestätigt wurde.</returns>
        public async Task<string?> BestaetigenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(() => BewegungBestaetigenAsync(lagerbewegungID, personID));
        }

        /// <summary>
        /// Methode, die mehrere offene Lagerbewegungen auf einmal bestätigt.
        /// </summary>
        /// <param name="lagerbewegungIDs">Die IDs der Lagerbewegungen.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Lagerbewegungen bestätigt wurden.</returns>
        public async Task<string?> BestaetigenAsync(IEnumerable<int> lagerbewegungIDs, int personID)
        {
            return await BuchenAsync(async () =>
            {
                // Doppelte IDs zählen nur einmal, sonst bräche die zweite als "bereits bestätigt" die Buchung ab.
                var ids = lagerbewegungIDs.Distinct().ToList();
                if (ids.Count == 0)
                {
                    return "Bitte mindestens eine Übernahme zum Bestätigen auswählen.";
                }

                foreach (var id in ids)
                {
                    var fehler = await BewegungBestaetigenAsync(id, personID);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung ablehnt. Die Menge wird im Von-Raum wieder zugebucht.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung abgelehnt wurde.</returns>
        public async Task<string?> AblehnenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
                var fehler = NichtOffenFehler(lagerbewegung);
                if (fehler != null)
                {
                    return fehler;
                }

                // Es gilt dieselbe Zuständigkeit wie beim Bestätigen.
                var zustaendigFehler = await FreigabeFehlerAsync(lagerbewegung!, personID, "ablehnen");
                if (zustaendigFehler != null)
                {
                    return zustaendigFehler;
                }

                return await StornierenAsync(lagerbewegung!);
            });
        }

        /// <summary>
        /// Methode, die eine noch nicht bestätigte Lagerbewegung zurückzieht. Die Menge wird im Von-Raum wieder zugebucht.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung zurückgezogen wurde.</returns>
        public async Task<string?> ZurueckziehenAsync(int lagerbewegungID, int personID)
        {
            return await BuchenAsync(async () =>
            {
                var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
                var fehler = NichtOffenFehler(lagerbewegung);
                if (fehler != null)
                {
                    return fehler;
                }

                // Zurückziehen darf, wer für den anfragenden Raum zuständig ist, bei einem Lager auch ein Admin.
                var anfrageRaum = lagerbewegung!.AnfrageRaum;
                if (!lagerbewegung.DarfZurueckziehen(personID, await IstAdminAsync(personID)))
                {
                    if (anfrageRaum.Raumart.IstLager)
                    {
                        return $"Nur die für das Lager {anfrageRaum.ID} verantwortliche Person oder ein Admin darf diese Lagerbewegung zurückziehen.";
                    }
                    return $"Nur die für den Raum {anfrageRaum.ID} verantwortliche Person darf diese Lagerbewegung zurückziehen.";
                }

                return await StornierenAsync(lagerbewegung);
            });
        }

        /// <summary>
        /// Methode, die den Bestand eines Raums korrigiert, zum Beispiel nach einer Inventur. Das darf nur ein Admin.
        /// </summary>
        /// <param name="raumID">Die ID des Raums.</param>
        /// <param name="positionen">Die geänderten Gegenstände (Schlüssel = GegenstandID) mit bisheriger und neuer Menge.</param>
        /// <param name="personID">Die ID des angemeldeten Admins.</param>
        /// <returns>Die Fehlermeldung oder null, wenn alle Korrekturen gebucht wurden.</returns>
        public async Task<string?> KorrigierenAsync(string raumID, IReadOnlyDictionary<int, (int Bisher, int Neu)> positionen, int personID)
        {
            return await BuchenAsync(async () =>
            {
                // Korrigieren darf nur ein Admin.
                if (!await _context.Person.AnyAsync(p => p.ID == personID && p.Rolle.Admin))
                {
                    return "Nur ein Admin darf den Bestand korrigieren.";
                }

                var raum = await _context.Raum.FindAsync(raumID);
                if (raum == null)
                {
                    return "Diesen Raum gibt es nicht.";
                }

                var korrektur = await _context.Bewegungsart.FirstOrDefaultAsync(b => b.Name == Bewegungsart.Korrektur);
                if (korrektur == null)
                {
                    return $"Die Bewegungsart \"{Bewegungsart.Korrektur}\" fehlt in der Datenbank. Sie wird mit dem SQL-Skript Testdaten.sql angelegt.";
                }

                var geaendert = positionen.Where(p => p.Value.Neu != p.Value.Bisher).ToList();
                if (geaendert.Count == 0)
                {
                    return "Es wurde keine Menge geändert.";
                }

                var jetzt = DateTime.Now;
                foreach (var (gegenstandID, (bisher, neu)) in geaendert)
                {
                    var fehler = await KorrekturBuchenAsync(gegenstandID, bisher, neu, raum.ID, korrektur.ID, personID, jetzt);
                    if (fehler != null)
                    {
                        return fehler;
                    }
                }

                return null;
            });
        }

        /// <summary>
        /// Methode, die die Menge eines Gegenstands in einem Raum korrigiert, ohne zu speichern.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands.</param>
        /// <param name="bisher">Die Menge, die beim Anzeigen im Raum war.</param>
        /// <param name="neu">Die neue Menge im Raum.</param>
        /// <param name="raumID">Die ID des Raums.</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart "Korrektur".</param>
        /// <param name="personID">Die ID des Admins.</param>
        /// <param name="jetzt">Der Zeitpunkt der Korrektur.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Korrektur gebucht wurde.</returns>
        private async Task<string?> KorrekturBuchenAsync(int gegenstandID, int bisher, int neu, string raumID, int bewegungsartID, int personID, DateTime jetzt)
        {
            var gegenstand = await _context.Gegenstand.FindAsync(gegenstandID);
            if (gegenstand == null)
            {
                return "Diesen Gegenstand gibt es nicht.";
            }

            if (neu < 0)
            {
                return $"Die Menge von \"{gegenstand.Name}\" darf nicht negativ sein.";
            }

            // Ein Gerät mit Seriennummer ist im Raum einmal oder gar nicht.
            if (gegenstand.Seriennummer != null && neu > 1)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist ein einzelnes Gerät und kann nur einmal oder gar nicht im Raum sein.";
            }

            // Hat sich der Bestand seit dem Anzeigen geändert, stimmt die gezählte Menge vielleicht nicht mehr, daher wird abgebrochen.
            var bestand = await _context.Raumbestand.FindAsync(gegenstand.ID, raumID);
            var aktuell = bestand?.Menge ?? 0;
            if (aktuell != bisher)
            {
                return $"Der Bestand von \"{gegenstand.Name}\" in Raum {raumID} hat sich inzwischen geändert (jetzt {aktuell} Stück). Bitte die Mengen prüfen und noch einmal speichern.";
            }

            var hoechstBestandFehler = await HoechstBestandFehlerAsync(gegenstand, (long)neu - aktuell);
            if (hoechstBestandFehler != null)
            {
                return hoechstBestandFehler;
            }

            // Ein Gerät mit Seriennummer darf danach nicht zweimal vorhanden sein.
            if (gegenstand.Seriennummer != null && neu == 1)
            {
                var hindernis = await GeraetVorhandenAsync(gegenstand);
                if (hindernis != null)
                {
                    return hindernis;
                }
            }

            if (bestand == null)
            {
                _context.Raumbestand.Add(new Raumbestand { GegenstandID = gegenstand.ID, RaumID = raumID, Menge = neu });
            }
            else if (neu == 0)
            {
                _context.Raumbestand.Remove(bestand);
            }
            else
            {
                bestand.Menge = neu;
            }

            _context.Lagerbewegung.Add(new Lagerbewegung
            {
                Menge = neu - aktuell,
                ErstelltAm = jetzt,
                BestaetigtAm = jetzt,
                BewegungsartID = bewegungsartID,
                VonRaumID = raumID,
                NachRaumID = raumID,
                GegenstandID = gegenstand.ID,
                PersonID = personID
            });
            return null;
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung prüft, anlegt und im Raumbestand bucht, ohne zu speichern.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands.</param>
        /// <param name="menge">Die Menge.</param>
        /// <param name="vonRaumID">Die ID des Von-Raums.</param>
        /// <param name="nachRaumID">Die ID des Nach-Raums.</param>
        /// <param name="bewegungsartID">Die ID der Bewegungsart.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <param name="festeBewegungsart">True, wenn der LagerService die Bewegungsart selbst vergibt.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung angelegt wurde.</returns>
        private async Task<string?> BewegungBuchenAsync(int gegenstandID, int menge, string vonRaumID, string nachRaumID, int bewegungsartID, int personID, bool festeBewegungsart = false)
        {
            var gegenstand = await _context.Gegenstand.FindAsync(gegenstandID);
            if (gegenstand == null)
            {
                return "Diesen Gegenstand gibt es nicht.";
            }

            if (menge < 1)
            {
                return $"Die Menge von \"{gegenstand.Name}\" muss mindestens 1 sein.";
            }

            // Ein Gerät mit Seriennummer wird immer einzeln gebucht.
            if (gegenstand.Seriennummer != null && menge != 1)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist ein einzelnes Gerät und kann nur mit der Menge 1 gebucht werden.";
            }

            var bewegungsart = await _context.Bewegungsart.FindAsync(bewegungsartID);
            if (bewegungsart == null)
            {
                return "Bitte eine Bewegungsart auswählen.";
            }

            // Sonst wäre die Bewegung zum Beispiel schon beim Anlegen storniert, aber trotzdem offen.
            var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
            if (nurVergebenFuer != null && !festeBewegungsart)
            {
                return $"Die Bewegungsart \"{bewegungsart.Name}\" bekommen nur {nurVergebenFuer}. Bitte eine andere auswählen.";
            }

            var vonRaum = await _context.Raum.Include(r => r.Raumart).FirstOrDefaultAsync(r => r.ID == vonRaumID);
            var nachRaum = await _context.Raum.Include(r => r.Raumart).FirstOrDefaultAsync(r => r.ID == nachRaumID);
            if (vonRaum == null || nachRaum == null)
            {
                return "Diesen Raum gibt es nicht.";
            }

            // Anlegen darf, wer für den Von-Raum buchen darf, und aus einem Lager auch die Person des Nach-Raums.
            var darfAusVonRaum = vonRaum.DarfBuchen(personID, await IstAdminAsync(personID));
            var ausLagerGeholt = !darfAusVonRaum && nachRaum.PersonID == personID && vonRaum.Raumart.IstLager;
            if (!darfAusVonRaum && !ausLagerGeholt)
            {
                if (nachRaum.PersonID == personID)
                {
                    return $"Raum {vonRaum.ID} ist kein Lager. Geräte holen kann man nur aus einem Lager, aus anderen Räumen muss die dort verantwortliche Person sie verlegen.";
                }
                return $"Nur die für den Raum {vonRaum.ID} verantwortliche Person darf Gegenstände aus diesem Raum buchen.";
            }

            // Freigeben muss die andere Seite: der Von-Raum, wenn die Person des Nach-Raums bucht, sonst der Nach-Raum.
            // Ein Wareneingang ist sofort bestätigt. Ein Admin bestätigt nicht automatisch, die Anfrage bleibt unter "Freigaben" offen.
            var wareneingang = vonRaum.ID == nachRaum.ID;
            Raum freigabeRaum;
            if (nachRaum.PersonID == personID)
            {
                freigabeRaum = vonRaum;
            }
            else
            {
                freigabeRaum = nachRaum;
            }
            var sofortBestaetigt = wareneingang || freigabeRaum.PersonID == personID;

            // Ohne verantwortliche Person könnte niemand freigeben und die Menge bliebe für immer unterwegs.
            if (!sofortBestaetigt && freigabeRaum.PersonID == null)
            {
                return $"Für den Raum {freigabeRaum.ID} ist niemand verantwortlich, der die Lagerbewegung freigeben könnte. Weise dem Raum zuerst eine verantwortliche Person zu.";
            }

            if (wareneingang)
            {
                var hoechstBestandFehler = await HoechstBestandFehlerAsync(gegenstand, menge);
                if (hoechstBestandFehler != null)
                {
                    return hoechstBestandFehler;
                }

                if (gegenstand.Seriennummer != null)
                {
                    var hindernis = await GeraetVorhandenAsync(gegenstand);
                    if (hindernis != null)
                    {
                        return hindernis;
                    }
                }
            }
            else
            {
                // Die Menge wird sofort abgebucht, damit sie nicht ein zweites Mal gebucht werden kann. Das ist die letzte Prüfung.
                var fehler = await AbbuchenAsync(gegenstand, vonRaum.ID, menge);
                if (fehler != null)
                {
                    return fehler;
                }
            }

            var jetzt = DateTime.Now;
            var lagerbewegung = new Lagerbewegung
            {
                Menge = menge,
                ErstelltAm = jetzt,
                BewegungsartID = bewegungsartID,
                VonRaumID = vonRaum.ID,
                NachRaumID = nachRaum.ID,
                GegenstandID = gegenstand.ID,
                PersonID = personID
            };
            _context.Lagerbewegung.Add(lagerbewegung);

            // Muss die Person nicht freigeben lassen, gilt die Bewegung sofort als bestätigt und wird im Nach-Raum zugebucht.
            if (sofortBestaetigt)
            {
                lagerbewegung.BestaetigtAm = jetzt;
                await ZubuchenAsync(gegenstand.ID, nachRaum.ID, menge);
            }

            return null;
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung prüft, bestätigt und im Nach-Raum zubucht, ohne zu speichern.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung bestätigt wurde.</returns>
        private async Task<string?> BewegungBestaetigenAsync(int lagerbewegungID, int personID)
        {
            var lagerbewegung = await LagerbewegungLadenAsync(lagerbewegungID);
            var fehler = NichtOffenFehler(lagerbewegung);
            if (fehler != null)
            {
                return fehler;
            }

            // Bestätigen darf, wer für den freigebenden Raum verantwortlich ist, bei einem Lager auch ein Admin.
            var zustaendigFehler = await FreigabeFehlerAsync(lagerbewegung!, personID, "bestätigen");
            if (zustaendigFehler != null)
            {
                return zustaendigFehler;
            }

            lagerbewegung!.BestaetigtAm = DateTime.Now;
            await ZubuchenAsync(lagerbewegung.GegenstandID, lagerbewegung.NachRaumID, lagerbewegung.Menge);

            return null;
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung storniert und die Menge im Von-Raum wieder zubucht, ohne zu speichern.
        /// </summary>
        /// <param name="lagerbewegung">Die offene Lagerbewegung.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung storniert wurde.</returns>
        private async Task<string?> StornierenAsync(Lagerbewegung lagerbewegung)
        {
            // Diese Bewegungsart kommt nur aus dem SQL-Skript Testdaten.sql.
            var storniert = await _context.Bewegungsart.FirstOrDefaultAsync(b => b.Name == Bewegungsart.Storniert);
            if (storniert == null)
            {
                return $"Die Bewegungsart \"{Bewegungsart.Storniert}\" fehlt in der Datenbank. Sie wird mit dem SQL-Skript Testdaten.sql angelegt.";
            }

            lagerbewegung.Bewegungsart = storniert;
            lagerbewegung.BestaetigtAm = DateTime.Now;
            await ZubuchenAsync(lagerbewegung.GegenstandID, lagerbewegung.VonRaumID, lagerbewegung.Menge);
            return null;
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung mit ihren Räumen und der Bewegungsart lädt.
        /// </summary>
        /// <param name="lagerbewegungID">Die ID der Lagerbewegung.</param>
        /// <returns>Die Lagerbewegung oder null, wenn es sie nicht gibt.</returns>
        private async Task<Lagerbewegung?> LagerbewegungLadenAsync(int lagerbewegungID)
        {
            // Die Raumart wird gebraucht, weil bei einem Lager auch ein Admin freigeben darf.
            return await _context.Lagerbewegung
                .Include(l => l.Bewegungsart)
                .Include(l => l.VonRaum).ThenInclude(r => r.Raumart)
                .Include(l => l.NachRaum).ThenInclude(r => r.Raumart)
                .FirstOrDefaultAsync(l => l.ID == lagerbewegungID);
        }

        /// <summary>
        /// Methode, die prüft, ob eine Person eine offene Lagerbewegung freigeben darf.
        /// </summary>
        /// <param name="lagerbewegung">Die geladene Lagerbewegung.</param>
        /// <param name="personID">Die ID der angemeldeten Person.</param>
        /// <param name="was">"bestätigen" oder "ablehnen" für die Fehlermeldung.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Person freigeben darf.</returns>
        private async Task<string?> FreigabeFehlerAsync(Lagerbewegung lagerbewegung, int personID, string was)
        {
            if (lagerbewegung.DarfFreigeben(personID, await IstAdminAsync(personID)))
            {
                return null;
            }

            var freigabeRaum = lagerbewegung.FreigabeRaum;
            if (!freigabeRaum.Raumart.IstLager)
            {
                return $"Nur die für den Raum {freigabeRaum.ID} verantwortliche Person darf diese Lagerbewegung {was}.";
            }
            return $"Nur die für das Lager {freigabeRaum.ID} verantwortliche Person oder ein Admin darf diese Lagerbewegung {was}.";
        }

        /// <summary>
        /// Methode, die prüft, ob es die Lagerbewegung gibt und ob sie noch offen ist.
        /// </summary>
        /// <param name="lagerbewegung">Die geladene Lagerbewegung oder null.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Lagerbewegung offen ist.</returns>
        private static string? NichtOffenFehler(Lagerbewegung? lagerbewegung)
        {
            if (lagerbewegung == null)
            {
                return "Diese Lagerbewegung gibt es nicht.";
            }

            if (lagerbewegung.BestaetigtAm == null)
            {
                return null;
            }

            var zeitpunkt = lagerbewegung.BestaetigtAm.Value.ToString("dd.MM.yyyy HH:mm");
            if (lagerbewegung.Storniert)
            {
                return $"Diese Lagerbewegung wurde bereits am {zeitpunkt} storniert.";
            }
            return $"Diese Lagerbewegung wurde bereits am {zeitpunkt} bestätigt.";
        }

        /// <summary>
        /// Methode, die eine Menge im Raumbestand abbucht, wenn genug vorhanden ist.
        /// </summary>
        /// <param name="gegenstand">Der Gegenstand.</param>
        /// <param name="raumID">Die ID des Raums.</param>
        /// <param name="menge">Die Menge.</param>
        /// <returns>Die Fehlermeldung oder null, wenn abgebucht wurde.</returns>
        private async Task<string?> AbbuchenAsync(Gegenstand gegenstand, string raumID, int menge)
        {
            var bestand = await _context.Raumbestand.FindAsync(gegenstand.ID, raumID);
            if (bestand == null || bestand.Menge < menge)
            {
                if (gegenstand.Seriennummer != null)
                {
                    return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist nicht im Raum {raumID}.";
                }
                return $"Im Raum {raumID} sind nur {bestand?.Menge ?? 0} Stück von \"{gegenstand.Name}\" vorhanden, {menge} Stück können daher nicht gebucht werden.";
            }

            bestand.Menge -= menge;
            if (bestand.Menge == 0)
            {
                _context.Raumbestand.Remove(bestand);
            }
            return null;
        }

        /// <summary>
        /// Methode, die eine Menge im Raumbestand zubucht.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands.</param>
        /// <param name="raumID">Die ID des Raums.</param>
        /// <param name="menge">Die Menge.</param>
        private async Task ZubuchenAsync(int gegenstandID, string raumID, int menge)
        {
            var bestand = await _context.Raumbestand.FindAsync(gegenstandID, raumID);
            if (bestand == null)
            {
                _context.Raumbestand.Add(new Raumbestand { GegenstandID = gegenstandID, RaumID = raumID, Menge = menge });
            }
            else
            {
                bestand.Menge += menge;
            }
        }

        /// <summary>
        /// Methode, die prüft, ob ein Gerät mit Seriennummer schon in einem Raum liegt oder unterwegs ist.
        /// </summary>
        /// <param name="gegenstand">Das Gerät mit Seriennummer.</param>
        /// <returns>Die Fehlermeldung oder null, wenn es das Gerät noch nicht gibt.</returns>
        private async Task<string?> GeraetVorhandenAsync(Gegenstand gegenstand)
        {
            var raumID = await _context.Raumbestand
                .Where(r => r.GegenstandID == gegenstand.ID && r.Menge > 0)
                .Select(r => r.RaumID)
                .FirstOrDefaultAsync();
            if (raumID != null)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist bereits im Raum {raumID}. Ein Gerät mit Seriennummer gibt es nur einmal.";
            }

            // Eine offene Lagerbewegung ist schon abgebucht, aber noch nicht im Nach-Raum zugebucht.
            var nachRaumID = await _context.Lagerbewegung
                .Where(l => l.BestaetigtAm == null)
                .Where(l => l.GegenstandID == gegenstand.ID)
                .Select(l => l.NachRaumID)
                .FirstOrDefaultAsync();
            if (nachRaumID != null)
            {
                return $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) ist gerade unterwegs in den Raum {nachRaumID}. Ein Gerät mit Seriennummer gibt es nur einmal.";
            }

            return null;
        }

        /// <summary>
        /// Methode, die prüft, ob ein Gegenstand nach einer Änderung den Höchstbestand überschreiten würde.
        /// </summary>
        /// <param name="gegenstand">Der Gegenstand.</param>
        /// <param name="aenderung">Die Änderung des Bestands in Stück.</param>
        /// <returns>Die Fehlermeldung oder null, wenn der Höchstbestand eingehalten wird.</returns>
        private async Task<string?> HoechstBestandFehlerAsync(Gegenstand gegenstand, long aenderung)
        {
            var inRaeumen = await _context.Raumbestand
                .Where(r => r.GegenstandID == gegenstand.ID)
                .SumAsync(r => (long)r.Menge);
            var unterwegs = await _context.Lagerbewegung
                .Where(l => l.BestaetigtAm == null)
                .Where(l => l.GegenstandID == gegenstand.ID)
                .SumAsync(l => (long)l.Menge);

            var bisher = inRaeumen + unterwegs;
            if (aenderung > 0 && bisher + aenderung > Gegenstand.HoechstBestand)
            {
                return $"Von \"{gegenstand.Name}\" kann es insgesamt höchstens {Gegenstand.HoechstBestand} Stück geben, jetzt sind es {bisher}. Diese Buchung würde den Bestand auf {bisher + aenderung} Stück bringen.";
            }

            return null;
        }

        /// <summary>
        /// Methode, die eine Buchung in einer Transaktion ausführt und speichert, wenn es keine Fehlermeldung gibt.
        /// </summary>
        /// <param name="buchung">Die Buchung, die die Daten prüft und ändert.</param>
        /// <returns>Die Fehlermeldung oder null, wenn die Buchung gespeichert wurde.</returns>
        private async Task<string?> BuchenAsync(Func<Task<string?>> buchung)
        {
            _context.ChangeTracker.Clear();

            try
            {
                await using var transaktion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // Bei einer Fehlermeldung wird die Transaktion ohne Speichern beendet.
                var fehler = await buchung();
                if (fehler != null)
                {
                    _context.ChangeTracker.Clear();
                    return fehler;
                }

                await _context.SaveChangesAsync();
                await transaktion.CommitAsync();
                return null;
            }
            catch (Exception exception)
            {
                _context.ChangeTracker.Clear();

                if (IstGleichzeitigeAenderung(exception))
                {
                    _logger.LogWarning(exception, "Buchung abgebrochen, weil gleichzeitig eine andere Buchung dieselben Daten geändert hat");
                    return "Die Buchung konnte nicht gespeichert werden, da gleichzeitig andere Buchungen dieselben Daten geändert haben. Bitte versuche es noch einmal.";
                }

                // Alles andere, zum Beispiel eine nicht erreichbare Datenbank, wird mit der Ursache protokolliert.
                _logger.LogError(exception, "Buchung konnte nicht gespeichert werden");
                return "Die Buchung konnte nicht gespeichert werden, weil ein unerwarteter Fehler aufgetreten ist. Es wurde nichts gebucht. Bitte versuche es noch einmal. Besteht das Problem weiter, melde es einem Admin.";
            }
        }

        /// <summary>
        /// Methode, die prüft, ob eine Buchung gescheitert ist, weil gleichzeitig eine andere Buchung dieselben Daten geändert hat.
        /// </summary>
        /// <param name="exception">Die Exception, die beim Buchen aufgetreten ist.</param>
        /// <returns>True, wenn eine gleichzeitige Änderung die Ursache ist.</returns>
        private static bool IstGleichzeitigeAenderung(Exception exception)
        {
            for (var aktuelle = exception; aktuelle != null; aktuelle = aktuelle.InnerException)
            {
                if (aktuelle is DbUpdateConcurrencyException || aktuelle is SqlException { Number: 1205 })
                {
                    return true;
                }
            }
            return false;
        }
    }
}
