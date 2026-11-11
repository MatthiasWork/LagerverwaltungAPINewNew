using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class RueckgabeController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der RueckgabeController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public RueckgabeController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die den Bestand des eigenen Raums als Checkliste zum Zurückgeben anzeigt. Die Seite sehen alle; wer keinen Raum hat
        /// oder selbst für das Umbuchungslager verantwortlich ist, bekommt nur einen Hinweis.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Rueckgabe
        public async Task<IActionResult> Index()
        {
            var raumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());
            var rueckgabe = new RueckgabeViewModel();
            await AnzeigeSetzenAsync(rueckgabe, raumID);
            return View(rueckgabe);
        }

        /// <summary>
        /// Methode, die die abgehakten Geräte aus dem eigenen Raum an das Umbuchungslager zurückgibt. Sie werden sofort abgebucht
        /// und kommen dort an, sobald die Person des Umbuchungslagers unter "Freigaben" bestätigt.
        /// </summary>
        /// <param name="rueckgabe">Das RueckgabeViewModel mit den Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Rueckgabe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("Mengen")] RueckgabeViewModel rueckgabe)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }
            var raumID = await _lagerService.RaumDerPersonAsync(personID);
            if (raumID == null)
            {
                return View("KeinRaum");
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht zurückgeben". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = rueckgabe.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.ZurueckgebenAsync(positionen, raumID, personID.Value);
                if (fehler == null)
                {
                    var umbuchungslager = await _context.Raum.Include(r => r.Person).FirstAsync(r => r.ID == Raum.Umbuchungslager);
                    var wartetAuf = umbuchungslager.Person?.VollerName ?? "die verantwortliche Person";
                    if (positionen.Count == 1)
                    {
                        TempData["Meldung"] = $"Rückgabe von 1 Gerät an das Umbuchungslager {umbuchungslager.ID} angefragt. Das Gerät ist unterwegs, bis {wartetAuf} die Rückgabe bestätigt.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"Rückgabe von {positionen.Count} Geräten an das Umbuchungslager {umbuchungslager.ID} angefragt. Die Geräte sind unterwegs, bis {wartetAuf} die Rückgabe bestätigt.";
                    }
                    return RedirectToAction("Index", "MeinRaum");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(rueckgabe, raumID);
            return View(rueckgabe);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige setzt: den eigenen Raum mit seinem Bestand und das Umbuchungslager mit der Person, die bestätigt.
        /// </summary>
        /// <param name="rueckgabe">Das RueckgabeViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums oder null, wenn die Person für keinen Raum verantwortlich ist</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(RueckgabeViewModel rueckgabe, string? raumID)
        {
            rueckgabe.Umbuchungslager = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .FirstOrDefaultAsync(r => r.ID == Raum.Umbuchungslager);

            if (raumID == null)
            {
                return;
            }

            rueckgabe.VonRaum = await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID);

            if (rueckgabe.KannZurueckgeben)
            {
                rueckgabe.Bestand = await _context.Raumbestand
                    .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                    .Where(r => r.RaumID == raumID)
                    .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                    .ToListAsync();
            }
        }
    }
}
