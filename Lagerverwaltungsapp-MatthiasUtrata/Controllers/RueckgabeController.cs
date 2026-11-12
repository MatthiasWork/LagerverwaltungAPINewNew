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
        /// Methode, die den Bestand eines eigenen Raums als Checkliste zum Zurückgeben anzeigt. Die Seite sehen alle; wer keinen Raum hat
        /// oder nur für das Umbuchungslager verantwortlich ist, bekommt nur einen Hinweis. Bei mehreren Räumen wählt die Person oben den Raum.
        /// </summary>
        /// <param name="vonRaumID">Die ID des eigenen Raums, aus dem zurückgegeben wird; ohne gültige ID der erste Raum der Person</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Rueckgabe
        public async Task<IActionResult> Index(string? vonRaumID)
        {
            var raeume = await _lagerService.RaeumeDerPersonAsync(User.GetPersonID());
            var rueckgabe = new RueckgabeViewModel { VonRaumID = vonRaumID };
            await AnzeigeSetzenAsync(rueckgabe, raeume);
            return View(rueckgabe);
        }

        /// <summary>
        /// Methode, die die abgehakten Geräte aus einem eigenen Raum an das Umbuchungslager zurückgibt. Sie werden sofort abgebucht
        /// und kommen dort an, sobald die Person des Umbuchungslagers unter "Freigaben" bestätigt.
        /// </summary>
        /// <param name="rueckgabe">Das RueckgabeViewModel mit Von-Raum und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Rueckgabe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("VonRaumID,Mengen")] RueckgabeViewModel rueckgabe)
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

            // Zurückgegeben wird nur aus einem Raum, für den die Person verantwortlich ist
            var raumID = LagerService.RaumAus(raeume, rueckgabe.VonRaumID)?.ID;
            if (raumID == null)
            {
                ModelState.AddModelError(string.Empty, "Bitte einen Raum auswählen, für den Sie verantwortlich sind.");
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht zurückgeben". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = rueckgabe.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.ZurueckgebenAsync(positionen, raumID!, personID.Value);
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
                    return RedirectToAction("Index", "MeinRaum", new { id = raumID });
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(rueckgabe, raeume);
            return View(rueckgabe);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige setzt: den gewählten eigenen Raum mit seinem Bestand, die Räume zur Auswahl und das
        /// Umbuchungslager mit der Person, die bestätigt.
        /// </summary>
        /// <param name="rueckgabe">Das RueckgabeViewModel, das angezeigt werden soll; VonRaumID ist danach der gewählte Raum, wie in der Datenbank geschrieben</param>
        /// <param name="raeume">Die Räume der Person, leer, wenn sie für keinen Raum verantwortlich ist</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(RueckgabeViewModel rueckgabe, List<Raum> raeume)
        {
            rueckgabe.Umbuchungslager = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .FirstOrDefaultAsync(r => r.ID == Raum.Umbuchungslager);

            // Aus dem Umbuchungslager selbst wird nicht zurückgegeben, es steht daher nicht zur Auswahl. Ist die Person nur für das
            // Umbuchungslager verantwortlich, bleibt es der Von-Raum, damit die Seite den passenden Hinweis zeigt
            rueckgabe.Raeume = raeume.Where(r => !Raum.IstUmbuchungslager(r.ID)).ToList();
            rueckgabe.VonRaum = LagerService.RaumAus(rueckgabe.Raeume, rueckgabe.VonRaumID)
                ?? rueckgabe.Raeume.FirstOrDefault()
                ?? raeume.FirstOrDefault();
            rueckgabe.VonRaumID = rueckgabe.VonRaum?.ID;

            if (rueckgabe.KannZurueckgeben)
            {
                var raumID = rueckgabe.VonRaum!.ID;
                rueckgabe.Bestand = await _context.Raumbestand
                    .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                    .Where(r => r.RaumID == raumID)
                    .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                    .ToListAsync();
            }
        }
    }
}
