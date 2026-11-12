using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class AusleiheController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der AusleiheController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public AusleiheController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die das Formular "Geräte verleihen" anzeigt. Verliehen wird aus dem eigenen Raum, ein Admin kann zusätzlich
        /// ein Lager wählen. Wer aus keinem Raum verleihen darf, bekommt nur einen Hinweis.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der schon ausgewählt sein soll (z. B. aus dem Gerätedetail); er muss im Von-Raum liegen</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem verliehen wird (ohne: der eigene Raum, sonst das erste Lager)</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Ausleihe
        public async Task<IActionResult> Index(int? gegenstandID, string? vonRaumID)
        {
            var ausleihe = new AusborgenViewModel
            {
                VonRaumID = vonRaumID,
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Ausgabe)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            if (!await AnzeigeSetzenAsync(ausleihe))
            {
                return View();
            }

            // Ein Gerät, das nicht im Von-Raum liegt, kann hier nicht verliehen werden und bleibt daher unausgewählt
            if (gegenstandID != null && ausleihe.Bestand.Any(b => b.GegenstandID == gegenstandID))
            {
                ausleihe.Mengen[gegenstandID.Value] = 1;
            }
            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die die ausgewählten Gegenstände aus dem Von-Raum an den gewählten Raum ausleiht.
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel mit Von-Raum, Zielraum, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Ausleihe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("VonRaumID,NachRaumID,BewegungsartID,Mengen")] AusborgenViewModel ausleihe)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            // Prüft auch, ob die Person aus dem gewählten Raum verleihen darf, und setzt sonst den eigenen Raum
            if (!await AnzeigeSetzenAsync(ausleihe))
            {
                return View("KeinRaum");
            }
            var vonRaum = ausleihe.VonRaum;

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht ausleihen". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = ausleihe.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.AusborgenAsync(positionen, vonRaum.ID, ausleihe.NachRaumID!, ausleihe.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var nachRaum = await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == ausleihe.NachRaumID);
                    var wartetAuf = nachRaum.Person?.VollerName ?? "die verantwortliche Person";
                    // An ein Lager kann ein Admin, auch der buchende, die Übernahme selbst unter "Freigaben" bestätigen (Raum.DarfBuchen)
                    if (nachRaum.Raumart.IstLager && User.IsInRole("Admin"))
                    {
                        wartetAuf += " oder ein Admin (auch Sie unter „Freigaben“)";
                    }
                    var ausEigenemRaum = vonRaum.ID == ausleihe.EigenerRaumID;
                    var aus = ausEigenemRaum ? "" : $" aus {vonRaum.ID}";
                    if (positionen.Count == 1)
                    {
                        TempData["Meldung"] = $"Verleih von 1 Gerät{aus} an Raum {nachRaum.ID} angefragt. Das Gerät ist unterwegs, bis {wartetAuf} die Übernahme bestätigt.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"Verleih von {positionen.Count} Geräten{aus} an Raum {nachRaum.ID} angefragt. Die Geräte sind unterwegs, bis {wartetAuf} die Übernahme bestätigt.";
                    }

                    // Aus dem eigenen Raum stehen die Anfragen unter "Mein Raum", aus einem Lager (Admin) unter "Freigaben"
                    if (ausEigenemRaum)
                    {
                        return RedirectToAction("Index", "MeinRaum");
                    }
                    return RedirectToAction("Index", "Uebernahme");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige des Formulars setzt: die Räume, aus denen verliehen werden darf, den gewählten mit
        /// seinem Bestand, die möglichen Zielräume und die Bewegungsarten.
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel, das angezeigt werden soll; VonRaumID ist danach der gewählte Raum, wie in der Datenbank geschrieben</param>
        /// <returns>False, wenn die Person aus keinem Raum verleihen darf</returns>
        private async Task<bool> AnzeigeSetzenAsync(AusborgenViewModel ausleihe)
        {
            ausleihe.EigenerRaumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());

            // Aus dem eigenen Raum und als Admin aus jedem Lager (Raum.DarfBuchen), der eigene Raum zuerst
            if (ausleihe.EigenerRaumID != null)
            {
                ausleihe.VonRaeume.Add(await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == ausleihe.EigenerRaumID));
            }
            if (User.IsInRole("Admin"))
            {
                ausleihe.VonRaeume.AddRange((await _lagerService.LagerAsync()).Where(r => r.ID != ausleihe.EigenerRaumID));
            }
            if (ausleihe.VonRaeume.Count == 0)
            {
                return false;
            }

            // Ein Raum, aus dem die Person nicht verleihen darf, wird nicht übernommen. Ohne Groß-/Kleinschreibung wie SQL Server
            ausleihe.VonRaum = ausleihe.VonRaeume.FirstOrDefault(r => string.Equals(r.ID, ausleihe.VonRaumID, StringComparison.OrdinalIgnoreCase))
                ?? ausleihe.VonRaeume[0];
            ausleihe.VonRaumID = ausleihe.VonRaum.ID;
            var vonRaumID = ausleihe.VonRaum.ID;

            ausleihe.Bestand = await _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == vonRaumID)
                .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                .ToListAsync();

            // Nur Räume mit einer verantwortlichen Person, denn diese muss die Übernahme bestätigen. Aus einem Lager nicht in den
            // eigenen Raum, das geht über "Aus dem Lager holen"
            ausleihe.Raeume = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.ID != vonRaumID && r.ID != ausleihe.EigenerRaumID && r.PersonID != null)
                .OrderBy(r => r.ID)
                .ToListAsync();

            ausleihe.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", ausleihe.BewegungsartID);
            return true;
        }
    }
}
