using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class LagerController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der LagerController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public LagerController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die die Lager zur Auswahl und den Bestand des gewählten Lagers anzeigt.
        /// </summary>
        /// <param name="id">Die ID des gewählten Lagers; ohne ID das erste</param>
        /// <param name="gegenstandID">Die ID des Gegenstands, der schon ausgewählt sein soll (z. B. aus dem Gerätedetail); er muss im gewählten Lager liegen</param>
        /// <param name="nachRaumID">Die ID des eigenen Raums, in den geholt wird; ohne gültige ID der erste Raum der Person</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Lager, Lager/Index/HL
        public async Task<IActionResult> Index(string? id, int? gegenstandID, string? nachRaumID)
        {
            var raeume = await _lagerService.RaeumeDerPersonAsync(User.GetPersonID());
            if (raeume.Count == 0)
            {
                return View("KeinRaum");
            }

            var holen = new HolenViewModel
            {
                VonRaumID = id,
                NachRaumID = (LagerService.RaumAus(raeume, nachRaumID) ?? raeume[0]).ID,
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Ausgabe)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(holen, raeume);

            // Ein Gerät, das nicht im gewählten Lager liegt, kann hier nicht geholt werden und bleibt daher unausgewählt
            if (gegenstandID != null && holen.Bestand.Any(b => b.GegenstandID == gegenstandID))
            {
                holen.Mengen[gegenstandID.Value] = 1;
            }
            return View(holen);
        }

        /// <summary>
        /// Methode, die die ausgewählten Geräte aus dem Lager für einen eigenen Raum anfragt. Sie werden sofort im Lager abgebucht
        /// (reserviert) und kommen in den eigenen Raum, sobald die Person des Lagers unter "Freigaben" freigibt.
        /// </summary>
        /// <param name="holen">Das HolenViewModel mit Lager, eigenem Raum, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Lager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("VonRaumID,NachRaumID,BewegungsartID,Mengen")] HolenViewModel holen)
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

            // Geholt wird nur in einen Raum, für den die Person verantwortlich ist
            var raumID = LagerService.RaumAus(raeume, holen.NachRaumID)?.ID;
            if (raumID == null)
            {
                ModelState.AddModelError(nameof(HolenViewModel.NachRaumID), "Bitte einen Raum auswählen, für den Sie verantwortlich sind.");
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht holen". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = holen.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                // Dieselbe Buchung wie bei der Ausleihe; der LagerService erlaubt sie, weil der Von-Raum ein Lager ist
                var fehler = await _lagerService.AusborgenAsync(positionen, holen.VonRaumID!, raumID!, holen.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var lager = await _context.Raum.Include(r => r.Person).FirstAsync(r => r.ID == holen.VonRaumID);
                    var wartetAuf = lager.Person?.VollerName ?? "die verantwortliche Person";
                    if (positionen.Count == 1)
                    {
                        TempData["Meldung"] = $"1 Gerät aus Lager {lager.ID} angefragt. Sobald {wartetAuf} freigibt, ist es in Raum {raumID}.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"{positionen.Count} Geräte aus Lager {lager.ID} angefragt. Sobald {wartetAuf} freigibt, sind sie in Raum {raumID}.";
                    }
                    return RedirectToAction("Index", "MeinRaum", new { id = raumID });
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(holen, raeume);
            return View(holen);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige setzt
        /// </summary>
        /// <param name="holen">Das HolenViewModel, das angezeigt werden soll; NachRaumID ist danach wie in der Datenbank geschrieben</param>
        /// <param name="raeume">Die Räume der Person, in die geholt werden kann</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(HolenViewModel holen, List<Raum> raeume)
        {
            holen.Raeume = raeume;
            holen.NachRaum = LagerService.RaumAus(raeume, holen.NachRaumID) ?? raeume[0];
            holen.NachRaumID = holen.NachRaum.ID;

            // Nur Lager mit verantwortlicher Person, denn diese muss freigeben. Eigene Lager nicht, dafür gibt es die Ausleihe
            var eigeneRaumIDs = raeume.Select(r => r.ID).ToList();
            holen.Lager = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.Raumart.Name.ToLower().Contains(Raumart.Lager) && r.PersonID != null && !eigeneRaumIDs.Contains(r.ID))
                .OrderBy(r => r.ID)
                .ToListAsync();

            // Ohne gültige Auswahl das erste Lager
            holen.VonRaum = holen.Lager.FirstOrDefault(r => string.Equals(r.ID, holen.VonRaumID, StringComparison.OrdinalIgnoreCase))
                ?? holen.Lager.FirstOrDefault();
            holen.VonRaumID = holen.VonRaum?.ID;

            if (holen.VonRaum != null)
            {
                holen.Bestand = await _context.Raumbestand
                    .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                    .Where(r => r.RaumID == holen.VonRaum.ID)
                    .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                    .ToListAsync();
            }

            holen.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", holen.BewegungsartID);
        }
    }
}
