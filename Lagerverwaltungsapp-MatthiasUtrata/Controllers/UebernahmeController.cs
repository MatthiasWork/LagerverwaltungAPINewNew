using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class UebernahmeController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
        /// </summary>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="lagerService">Der Service für Zuständigkeiten und Buchungen.</param>
        public UebernahmeController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die anzeigt, welche Lagerbewegungen die angemeldete Person freigeben muss und welche zuletzt erledigt wurden.
        /// Ein Admin sieht zusätzlich die Anfragen für alle Lager.
        /// </summary>
        /// <returns>Die Seite mit den Freigaben.</returns>
        // GET: Uebernahme
        public async Task<IActionResult> Index()
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var istAdmin = User.IsInRole("Admin");

            // Die eigenen Räume, für die freigegeben wird.
            var uebernahme = new UebernahmeViewModel
            {
                Raeume = await _lagerService.RaeumeDerPersonAsync(personID)
            };
            var raumIDs = uebernahme.Raeume.Select(r => r.ID).ToList();
            if (istAdmin)
            {
                uebernahme.Lager = (await _lagerService.LagerAsync()).Where(r => !raumIDs.Contains(r.ID)).ToList();
            }
            if (!uebernahme.GibtFrei)
            {
                return View(uebernahme);
            }

            uebernahme.Offen = await _context.Lagerbewegung
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .Include(l => l.Person)
                .Include(l => l.VonRaum).ThenInclude(r => r.Raumart)
                .Include(l => l.NachRaum).ThenInclude(r => r.Raumart)
                .Where(Lagerbewegung.IstOffen)
                .Where(Lagerbewegung.FreigabeDurch(personID.Value, istAdmin))
                .OrderBy(l => l.ErstelltAm)
                .ToListAsync();

            // Bestätigt oder storniert; ein Wareneingang ist keine Freigabe und fehlt daher.
            uebernahme.Erledigt = await _context.Lagerbewegung
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .Where(l => l.BestaetigtAm != null && l.VonRaumID != l.NachRaumID)
                .Where(Lagerbewegung.FreigabeDurch(personID.Value, istAdmin))
                .OrderByDescending(l => l.BestaetigtAm)
                .Take(10)
                .ToListAsync();

            if (istAdmin)
            {
                // Vom Admin aus einem Lager verliehen. Anfragen an ein Lager stehen schon oben unter den Freigaben.
                uebernahme.Angefragt = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.VonRaum).ThenInclude(r => r.Person)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                    .Where(Lagerbewegung.IstOffen)
                    .Where(l => l.PersonID == personID && l.NachRaum.PersonID != personID && !raumIDs.Contains(l.VonRaumID)
                        && !l.NachRaum.Raumart.Name.ToLower().Contains(Raumart.Lager))
                    .OrderByDescending(l => l.ErstelltAm)
                    .ToListAsync();
            }

            return View(uebernahme);
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung zurückzieht, die ein Admin aus einem Lager angefragt hat.
        /// Die Menge geht zurück in das Lager.
        /// </summary>
        /// <param name="id">Die ID der Lagerbewegung.</param>
        /// <returns>Die Weiterleitung zur Übersicht der Freigaben.</returns>
        // POST: Uebernahme/Zurueckziehen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zurueckziehen(int id)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.ZurueckziehenAsync(id, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var vonRaumID = await _context.Lagerbewegung.Where(l => l.ID == id).Select(l => l.VonRaumID).FirstAsync();
                TempData["Meldung"] = $"Die Anfrage wurde zurückgezogen. Das Gerät ist wieder im Bestand von Raum {vonRaumID}.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die eine oder mehrere offene Lagerbewegungen freigibt. Die Geräte kommen damit in den Nach-Raum.
        /// </summary>
        /// <param name="ids">Die IDs der Lagerbewegungen.</param>
        /// <returns>Die Weiterleitung zur Übersicht der Freigaben.</returns>
        // POST: Uebernahme/Bestaetigen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bestaetigen(int[] ids)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.BestaetigenAsync(ids, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var nachRaumIDs = await _context.Lagerbewegung.Where(l => ids.Contains(l.ID)).Select(l => l.NachRaumID).Distinct().ToListAsync();
                string wo;
                if (nachRaumIDs.Count == 1)
                {
                    wo = $"im Bestand von Raum {nachRaumIDs[0]}";
                }
                else
                {
                    wo = $"in den Räumen {string.Join(", ", nachRaumIDs.Order())}";
                }

                var anzahl = ids.Distinct().Count();
                if (anzahl == 1)
                {
                    TempData["Meldung"] = $"Die Anfrage wurde freigegeben. Das Gerät ist jetzt {wo}.";
                }
                else
                {
                    TempData["Meldung"] = $"{anzahl} Anfragen wurden freigegeben. Die Geräte sind jetzt {wo}.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die eine offene Lagerbewegung ablehnt. Die Menge geht zurück in den Von-Raum.
        /// </summary>
        /// <param name="id">Die ID der Lagerbewegung.</param>
        /// <returns>Die Weiterleitung zur Übersicht der Freigaben.</returns>
        // POST: Uebernahme/Ablehnen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ablehnen(int id)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.AblehnenAsync(id, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var vonRaumID = await _context.Lagerbewegung.Where(l => l.ID == id).Select(l => l.VonRaumID).FirstAsync();
                TempData["Meldung"] = $"Die Anfrage wurde abgelehnt. Das Gerät ist wieder im Bestand von Raum {vonRaumID}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
