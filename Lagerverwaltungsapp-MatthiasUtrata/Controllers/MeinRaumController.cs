using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class MeinRaumController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
        /// </summary>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="lagerService">Der Service für Zuständigkeiten und Buchungen.</param>
        public MeinRaumController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die einen Raum der angemeldeten Person mit Bestand und offenen Lagerbewegungen anzeigt.
        /// Der Bestand kann nach Suchbegriff und Kategorie gefiltert werden.
        /// </summary>
        /// <param name="id">Die ID des Raums; ohne gültige ID der erste Raum der Person.</param>
        /// <param name="suche">Der Suchbegriff für Bezeichnung oder Seriennummer.</param>
        /// <param name="kategorieID">Die ID der Kategorie für den Filter.</param>
        /// <returns>Die Seite mit dem Raumbestand.</returns>
        // GET: MeinRaum, MeinRaum/Index/101
        public async Task<IActionResult> Index(string? id, string? suche, int? kategorieID)
        {
            var raeume = await _lagerService.RaeumeDerPersonAsync(User.GetPersonID());
            if (raeume.Count == 0)
            {
                return View("KeinRaum");
            }
            var raum = LagerService.RaumAus(raeume, id) ?? raeume[0];
            var raumID = raum.ID;

            suche = suche?.Trim();

            var abfrage = _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == raumID);

            if (!string.IsNullOrEmpty(suche))
            {
                abfrage = abfrage.Where(r => r.Gegenstand.Name.Contains(suche)
                    || (r.Gegenstand.Seriennummer != null && r.Gegenstand.Seriennummer.Contains(suche)));
            }

            if (kategorieID != null)
            {
                abfrage = abfrage.Where(r => r.Gegenstand.KategorieID == kategorieID);
            }

            var meinRaum = new MeinRaumViewModel
            {
                Raum = raum,
                Raeume = raeume,
                Bestand = await abfrage.OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer).ToListAsync(),
                AnzahlGesamt = await _context.Raumbestand.CountAsync(r => r.RaumID == raumID),
                StueckGesamt = await _context.Raumbestand.Where(r => r.RaumID == raumID).SumAsync(r => r.Menge),
                // Angefragte Lagerbewegungen, die die Person zurückziehen kann.
                Unterwegs = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.VonRaum).ThenInclude(r => r.Person)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                    .Where(l => l.BestaetigtAm == null)
                    .Where(Lagerbewegung.AngefragtFuer(raumID))
                    .OrderByDescending(l => l.ErstelltAm)
                    .ToListAsync(),
                OffeneUebernahmen = await _context.Lagerbewegung
                    .Where(l => l.BestaetigtAm == null)
                    .Where(Lagerbewegung.FreigabeFuer(raumID))
                    .CountAsync(),
                Suche = suche,
                KategorieID = kategorieID,
                // Nur Kategorien, die im Raum vorkommen.
                Kategorien = new SelectList(await _context.Kategorie
                    .Where(k => k.Gegenstand.Any(g => g.Raumbestand.Any(r => r.RaumID == raumID)))
                    .OrderBy(k => k.Name)
                    .ToListAsync(), "ID", "Name", kategorieID)
            };

            return View(meinRaum);
        }

        /// <summary>
        /// Methode, die eine Lagerbewegung zurückzieht, die die angemeldete Person angefragt hat.
        /// </summary>
        /// <param name="id">Die ID der Lagerbewegung.</param>
        /// <param name="raumID">Die ID des Raums, der danach angezeigt wird.</param>
        /// <returns>Die Weiterleitung zurück zum Raum.</returns>
        // POST: MeinRaum/Zurueckziehen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zurueckziehen(int id, string? raumID)
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

            return RedirectToAction(nameof(Index), new { id = raumID });
        }

        /// <summary>
        /// Methode, die das Formular für einen Wareneingang in einen eigenen Raum anzeigt.
        /// </summary>
        /// <param name="gegenstandID">Die ID des vorausgewählten Gegenstands.</param>
        /// <param name="raumID">Die ID des vorausgewählten Raums; ohne gültige ID der erste Raum der Person.</param>
        /// <returns>Die Seite mit dem Wareneingangsformular.</returns>
        // GET: MeinRaum/Wareneingang
        public async Task<IActionResult> Wareneingang(int? gegenstandID, string? raumID)
        {
            var raeume = await _lagerService.RaeumeDerPersonAsync(User.GetPersonID());
            if (raeume.Count == 0)
            {
                return View("KeinRaum");
            }

            var wareneingang = new WareneingangViewModel
            {
                GegenstandID = gegenstandID,
                RaumID = (LagerService.RaumAus(raeume, raumID) ?? raeume[0]).ID,
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Wareneingang)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(wareneingang, raeume);
            return View(wareneingang);
        }

        /// <summary>
        /// Methode, die einen Wareneingang in einen eigenen Raum bucht. Die Lagerbewegung ist sofort bestätigt,
        /// da die Person für den Raum verantwortlich ist.
        /// </summary>
        /// <param name="wareneingang">Die Eingaben aus dem Formular.</param>
        /// <returns>Die Weiterleitung zum Raum oder bei einem Fehler das Formular.</returns>
        // POST: MeinRaum/Wareneingang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Wareneingang([Bind("RaumID,GegenstandID,Menge,BewegungsartID")] WareneingangViewModel wareneingang)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }
            var raeume = await _lagerService.RaeumeDerPersonAsync(personID);
            if (raeume.Count == 0)
            {
                return View("KeinRaum");
            }

            // Eingebucht wird nur in einen Raum, für den die Person verantwortlich ist.
            var raum = LagerService.RaumAus(raeume, wareneingang.RaumID);
            if (raum == null)
            {
                ModelState.AddModelError(nameof(WareneingangViewModel.RaumID), "Bitte einen Raum auswählen, für den Sie verantwortlich sind.");
            }

            if (ModelState.IsValid)
            {
                var raumID = raum!.ID;
                var fehler = await _lagerService.WareneingangAsync(wareneingang.GegenstandID!.Value, wareneingang.Menge!.Value, raumID,
                    wareneingang.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var gegenstand = await _context.Gegenstand.FirstAsync(g => g.ID == wareneingang.GegenstandID);
                    if (gegenstand.Seriennummer != null)
                    {
                        TempData["Meldung"] = $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) wurde in Raum {raumID} eingebucht.";
                    }
                    else if (wareneingang.Menge == 1)
                    {
                        TempData["Meldung"] = $"1 Stück \"{gegenstand.Name}\" wurde in Raum {raumID} eingebucht.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"{wareneingang.Menge} Stück \"{gegenstand.Name}\" wurden in Raum {raumID} eingebucht.";
                    }
                    return RedirectToAction(nameof(Index), new { id = raumID });
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(wareneingang, raeume);
            return View(wareneingang);
        }

        /// <summary>
        /// Methode, die das Wareneingangsformular mit den Auswahllisten befüllt.
        /// </summary>
        /// <param name="wareneingang">Das ViewModel des Formulars.</param>
        /// <param name="raeume">Die Räume der Person.</param>
        private async Task AnzeigeSetzenAsync(WareneingangViewModel wareneingang, List<Raum> raeume)
        {
            wareneingang.Raeume = raeume;
            wareneingang.Raum = LagerService.RaumAus(raeume, wareneingang.RaumID) ?? raeume[0];
            wareneingang.RaumID = wareneingang.Raum.ID;

            // Ein Gerät mit Seriennummer gibt es nur einmal; liegt es schon in einem Raum oder ist es unterwegs, kann es nicht eingehen.
            wareneingang.Gegenstaende = await _context.Gegenstand
                .Include(g => g.Kategorie)
                .Where(g => g.Seriennummer == null
                    || (!g.Raumbestand.Any() && !g.Lagerbewegung.Any(l => l.BestaetigtAm == null)))
                .OrderBy(g => g.Name).ThenBy(g => g.Seriennummer)
                .ToListAsync();

            wareneingang.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", wareneingang.BewegungsartID);
        }
    }
}
