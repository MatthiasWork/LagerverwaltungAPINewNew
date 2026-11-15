using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly LagerverwaltungContext _context;

        private readonly PasswordService _passwordService;

        private readonly LagerService _lagerService;

        private readonly LoginSperreService _loginSperreService;

        /// <summary>
        /// Konstruktor, der Logger, Datenbankkontext und die benötigten Services übernimmt.
        /// </summary>
        /// <param name="logger">Der Logger.</param>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="passwordService">Der Service für Passwörter.</param>
        /// <param name="lagerService">Der Service für die Zuständigkeiten der Räume.</param>
        /// <param name="loginSperreService">Der Service für die Sperre nach fehlgeschlagenen Anmeldungen.</param>
        public HomeController(ILogger<HomeController> logger, LagerverwaltungContext context, PasswordService passwordService, LagerService lagerService,
            LoginSperreService loginSperreService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
            _lagerService = lagerService;
            _loginSperreService = loginSperreService;
        }

        /// <summary>
        /// Methode, die die Startseite mit Kennzahlen, Bestand und letzten Buchungen anzeigt.
        /// Admins sehen alle Buchungen, alle anderen nur ihre eigenen und die ihres Raums.
        /// </summary>
        /// <returns>Die Startseite.</returns>
        // GET: Home
        public async Task<IActionResult> Index()
        {
            var personID = User.GetPersonID();
            var istAdmin = User.IsInRole("Admin");
            var raumIDs = (await _lagerService.RaeumeDerPersonAsync(personID)).Select(r => r.ID).ToList();

            var offen = _context.Lagerbewegung.Where(l => l.BestaetigtAm == null);
            var uebersicht = new UebersichtViewModel
            {
                Verfuegbar = await _context.Raumbestand.SumAsync(r => r.Menge)
            };
            // Unterwegs ist, was abgebucht, aber noch nicht übernommen wurde.
            uebersicht.Gesamt = uebersicht.Verfuegbar + await offen.SumAsync(l => l.Menge);

            // Die verfügbaren Stück werden nach Kategorie und nach Raum aufgeteilt.
            uebersicht.BestandJeKategorie = await _context.Raumbestand
                .GroupBy(r => new { r.Gegenstand.KategorieID, r.Gegenstand.Kategorie.Name })
                .Where(g => g.Sum(r => r.Menge) > 0)
                .OrderByDescending(g => g.Sum(r => r.Menge))
                .ThenBy(g => g.Key.Name)
                .Select(g => new KategorieStatistik { KategorieID = g.Key.KategorieID, Name = g.Key.Name, Stueck = g.Sum(r => r.Menge) })
                .ToListAsync();

            uebersicht.BestandJeRaum = await _context.Raumbestand
                .GroupBy(r => new { r.RaumID, Raumart = r.Raum.Raumart.Name })
                .Where(g => g.Sum(r => r.Menge) > 0)
                .OrderByDescending(g => g.Sum(r => r.Menge))
                .ThenBy(g => g.Key.RaumID)
                .Select(g => new RaumStatistik { RaumID = g.Key.RaumID, Raumart = g.Key.Raumart, Stueck = g.Sum(r => r.Menge) })
                .ToListAsync();

            // Admins sehen alle offenen Freigaben, alle anderen nur die für ihre Räume.
            if (istAdmin)
            {
                uebersicht.OffeneFreigaben = await offen.CountAsync();
            }
            else if (personID != null && raumIDs.Count > 0)
            {
                uebersicht.OffeneFreigaben = await offen.Where(Lagerbewegung.FreigabeDurch(personID.Value, false)).CountAsync();
            }

            var bewegungen = _context.Lagerbewegung.AsQueryable();
            if (!istAdmin)
            {
                bewegungen = bewegungen.Where(l => l.PersonID == personID || raumIDs.Contains(l.VonRaumID) || raumIDs.Contains(l.NachRaumID));
            }

            // Jede Bewegung ergibt bis zu zwei Einträge, daher reichen die 5 neuesten Bewegungen für die 5 neuesten Einträge.
            var letzte = await bewegungen
                .Include(l => l.Person)
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .OrderByDescending(l => l.BestaetigtAm ?? l.ErstelltAm)
                .Take(5)
                .ToListAsync();

            foreach (var l in letzte)
            {
                var person = l.Person.VollerName;
                // Bei einer Korrektur kann die Menge negativ sein, die Richtung steht im Text.
                var menge = Math.Abs(l.Menge);
                string was;
                if (menge == 1)
                {
                    was = l.Gegenstand.Name;
                }
                else
                {
                    was = $"{menge} × {l.Gegenstand.Name}";
                }

                if (l.IstKorrektur)
                {
                    string text;
                    if (l.Menge > 0)
                    {
                        text = $"{person} hat {was} in {l.NachRaumID} eingebucht (Korrektur)";
                    }
                    else
                    {
                        text = $"{person} hat {was} aus {l.NachRaumID} ausgebucht (Korrektur)";
                    }
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = text });
                    continue;
                }

                // Bei einer stornierten Bewegung steht in BestaetigtAm der Zeitpunkt der Stornierung, daher wird Storniert zuerst geprüft.
                if (!l.Storniert && l.BestaetigtAm == l.ErstelltAm)
                {
                    // Sofort bestätigt ist ein Wareneingang oder eine Umbuchung zwischen zwei eigenen Räumen.
                    string text;
                    if (l.VonRaumID == l.NachRaumID)
                    {
                        text = $"{person} hat {was} in {l.NachRaumID} eingebucht";
                    }
                    else
                    {
                        text = $"{person} hat {was} von {l.VonRaumID} nach {l.NachRaumID} umgebucht";
                    }
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = text });
                    continue;
                }

                // Eine Rückgabe geht immer an das Umbuchungslager, alles andere ist eine Ausleihe.
                var art = l.IstRueckgabe ? "Rückgabe" : "Ausleihe";
                uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = $"{person} fragt {art} von {was} nach {l.NachRaumID} an" });
                if (l.Storniert)
                {
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm!.Value, Text = $"Ausleihe von {was} nach {l.NachRaumID} storniert, zurück in {l.VonRaumID}" });
                }
                else if (l.BestaetigtAm != null)
                {
                    uebersicht.Aktivitaeten.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm.Value, Text = $"{was} in {l.NachRaumID} übernommen" });
                }
            }

            uebersicht.Aktivitaeten = uebersicht.Aktivitaeten.OrderByDescending(a => a.Zeitpunkt).Take(5).ToList();

            return View(uebersicht);
        }

        /// <summary>
        /// Methode, die die Login-Seite anzeigt. Gibt es noch keinen Benutzer, geht es stattdessen zur Registrierung.
        /// </summary>
        /// <param name="returnUrl">Die URL, zu der nach dem Login weitergeleitet wird.</param>
        /// <returns>Die Login-Seite oder eine Weiterleitung.</returns>
        // GET: Home/Login
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl)
        {
            // Wer angemeldet ist, braucht das Formular nicht.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Register));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Methode, die einen Benutzer anmeldet, wenn Benutzername und Passwort stimmen.
        /// Nach zu vielen Fehlschlägen ist der Benutzername für eine Zeit gesperrt.
        /// </summary>
        /// <param name="login">Benutzername und Passwort aus dem Formular.</param>
        /// <param name="returnUrl">Die URL, zu der nach dem Login weitergeleitet wird.</param>
        /// <returns>Die Weiterleitung nach erfolgreichem Login, sonst die Login-Seite.</returns>
        // POST: Home/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel login, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(login);
            }

            var person = await _context.Person
                .Include(p => p.Rolle)
                .FirstOrDefaultAsync(p => p.Username == login.Username);

            // Der Zähler gehört zum gefundenen Benutzer, damit andere Schreibweisen des Namens die Sperre nicht umgehen.
            var sperrName = person?.Username ?? login.Username;

            // Der Versuch zählt schon vor der Prüfung, auch bei unbekannten Namen, sonst verriete die Sperre, welche Benutzer es gibt.
            var restzeit = _loginSperreService.VersuchBeginnen(sperrName);
            if (restzeit != null)
            {
                ViewData["SperreSekunden"] = SperreSekunden(restzeit.Value);
                return View(login);
            }

            if (person == null || !_passwordService.VerifyPassword(person, login.Password))
            {
                // Hat dieser Fehlschlag die Sperre ausgelöst, läuft ab jetzt der Countdown.
                var sperre = _loginSperreService.RestzeitDerSperre(sperrName);
                if (sperre != null)
                {
                    ViewData["SperreSekunden"] = SperreSekunden(sperre.Value);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Benutzername oder Passwort ist falsch.");
                }
                return View(login);
            }

            _loginSperreService.ErfolgMelden(sperrName);
            await AnmeldenAsync(person);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die die Restzeit einer Sperre in ganze Sekunden umrechnet. Sie rundet auf, damit das Formular nie früher frei wird als die Sperre endet.
        /// </summary>
        /// <param name="restzeit">Die Restzeit der Sperre.</param>
        /// <returns>Die Restzeit in Sekunden, mindestens 1.</returns>
        private static int SperreSekunden(TimeSpan restzeit)
        {
            return Math.Max(1, (int)Math.Ceiling(restzeit.TotalSeconds));
        }

        /// <summary>
        /// Methode, die die Registrierungsseite anzeigt. Registrieren kann sich nur der erste Benutzer, alle weiteren legt ein Admin an.
        /// </summary>
        /// <returns>Die Registrierungsseite oder eine Weiterleitung.</returns>
        // GET: Home/Register
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Login));
            }
            return View();
        }

        /// <summary>
        /// Methode, die den ersten Benutzer als Admin registriert und direkt anmeldet.
        /// </summary>
        /// <param name="registrierung">Die Daten aus dem Formular.</param>
        /// <returns>Die Weiterleitung zur Startseite oder zum Login, bei Fehlern das Formular.</returns>
        // POST: Home/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registrierung)
        {
            // Auch hier wird geprüft, sonst könnte man sich später per direkt abgeschicktem Formular als Admin registrieren.
            if (await _context.Person.AnyAsync())
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View(registrierung);
            }

            // Der erste Benutzer wird Admin, damit er weitere Benutzer anlegen kann. Gibt es noch keine Admin-Rolle, wird sie angelegt.
            var adminRolle = await _context.Rolle.FirstOrDefaultAsync(r => r.Admin)
                ?? new Rolle { Name = "Administrator", Admin = true };

            var person = new Person
            {
                Vorname = registrierung.Vorname,
                Nachname = registrierung.Nachname,
                Username = registrierung.Username,
                Email = registrierung.Email,
                Rolle = adminRolle
            };
            person.Password = _passwordService.HashPassword(person, registrierung.Password);

            _context.Person.Add(person);
            await _context.SaveChangesAsync();

            await AnmeldenAsync(person);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die den Benutzer abmeldet.
        /// </summary>
        /// <returns>Die Weiterleitung zur Login-Seite.</returns>
        // POST: Home/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Methode, die die Seite für verweigerten Zugriff anzeigt.
        /// </summary>
        /// <returns>Die Seite für verweigerten Zugriff.</returns>
        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Methode, die eine Person per Cookie anmeldet.
        /// </summary>
        /// <param name="person">Die Person mit geladener Rolle.</param>
        private async Task AnmeldenAsync(Person person)
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, person.ErstelleClaimsPrincipal());
        }

        /// <summary>
        /// Methode, die die Fehlerseite anzeigt.
        /// </summary>
        /// <returns>Die Fehlerseite.</returns>
        // GET: Home/Error
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Methode, die statt einer leeren Seite einen Text zum HTTP-Fehlercode anzeigt.
        /// </summary>
        /// <param name="id">Der HTTP-Fehlercode.</param>
        /// <returns>Die Fehlerseite mit dem passenden Text.</returns>
        // GET: Home/Fehlercode/404
        [AllowAnonymous]
        public IActionResult Fehlercode(int id)
        {
            if (id == StatusCodes.Status404NotFound)
            {
                ViewData["Title"] = "Seite nicht gefunden";
                ViewData["Text"] = "Diese Seite oder diesen Eintrag gibt es nicht (mehr). Vielleicht wurde der Eintrag inzwischen gelöscht oder die Adresse stimmt nicht.";
            }
            else if (id == StatusCodes.Status400BadRequest)
            {
                // Das passiert zum Beispiel, wenn das Sicherheitstoken eines Formulars abgelaufen ist.
                ViewData["Title"] = "Anfrage nicht möglich";
                ViewData["Text"] = "Die Anfrage konnte nicht verarbeitet werden. Vielleicht war die Seite zu lange geöffnet. Bitte lade sie neu und versuche es noch einmal.";
            }
            else
            {
                ViewData["Title"] = "Fehler";
                ViewData["Text"] = $"Die Anfrage konnte nicht verarbeitet werden (Fehlercode {id}).";
            }
            return View();
        }
    }
}
