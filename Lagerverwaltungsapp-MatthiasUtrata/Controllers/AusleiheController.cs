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
        /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
        /// </summary>
        /// <param name="context">Der Datenbankkontext.</param>
        /// <param name="lagerService">Der Service für Zuständigkeiten und Buchungen.</param>
        public AusleiheController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die das Formular "Geräte verleihen" anzeigt. Verliehen wird aus einem eigenen Raum, ein Admin kann auch ein Lager wählen.
        /// </summary>
        /// <param name="gegenstandID">Die ID des vorausgewählten Gegenstands.</param>
        /// <param name="vonRaumID">Die ID des Raums, aus dem verliehen wird.</param>
        /// <returns>Die Seite mit dem Verleihformular.</returns>
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

            // Ein Gerät, das nicht im Von-Raum liegt, bleibt unausgewählt.
            if (gegenstandID != null && ausleihe.Bestand.Any(b => b.GegenstandID == gegenstandID))
            {
                ausleihe.Mengen[gegenstandID.Value] = 1;
            }
            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die die ausgewählten Gegenstände aus dem Von-Raum an den gewählten Raum verleiht.
        /// </summary>
        /// <param name="ausleihe">Die Eingaben aus dem Formular.</param>
        /// <returns>Die Weiterleitung zur Übersicht oder bei einem Fehler das Formular.</returns>
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

            // Prüft auch, ob die Person aus dem gewählten Raum verleihen darf.
            if (!await AnzeigeSetzenAsync(ausleihe))
            {
                return View("KeinRaum");
            }
            var vonRaum = ausleihe.VonRaum;

            if (ModelState.IsValid)
            {
                // Leer oder 0 heißt "nicht ausleihen"; negative Mengen lehnt der LagerService ab.
                var positionen = ausleihe.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.AusborgenAsync(positionen, vonRaum.ID, ausleihe.NachRaumID!, ausleihe.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var nachRaum = await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == ausleihe.NachRaumID);
                    var ausEigenemRaum = ausleihe.EigeneRaumIDs.Contains(vonRaum.ID);
                    var geraete = positionen.Count == 1 ? "1 Gerät" : $"{positionen.Count} Geräte";

                    if (nachRaum.PersonID == personID)
                    {
                        // Zwischen zwei eigenen Räumen braucht es keine Freigabe.
                        TempData["Meldung"] = $"{geraete} von Raum {vonRaum.ID} nach Raum {nachRaum.ID} umgebucht.";
                    }
                    else
                    {
                        var wartetAuf = nachRaum.Person?.VollerName ?? "die verantwortliche Person";
                        // An ein Lager kann auch ein Admin die Übernahme unter "Freigaben" bestätigen.
                        if (nachRaum.Raumart.IstLager && User.IsInRole("Admin"))
                        {
                            wartetAuf += " oder ein Admin (auch Sie unter „Freigaben“)";
                        }
                        var aus = ausleihe.EigeneRaumIDs.Count == 1 && ausEigenemRaum ? "" : $" aus {vonRaum.ID}";
                        if (positionen.Count == 1)
                        {
                            TempData["Meldung"] = $"Verleih von 1 Gerät{aus} an Raum {nachRaum.ID} angefragt. Das Gerät ist unterwegs, bis {wartetAuf} die Übernahme bestätigt.";
                        }
                        else
                        {
                            TempData["Meldung"] = $"Verleih von {positionen.Count} Geräten{aus} an Raum {nachRaum.ID} angefragt. Die Geräte sind unterwegs, bis {wartetAuf} die Übernahme bestätigt.";
                        }
                    }

                    // Anfragen aus einem eigenen Raum stehen unter "Mein Raum", aus einem Lager unter "Freigaben".
                    if (ausEigenemRaum)
                    {
                        return RedirectToAction("Index", "MeinRaum", new { id = vonRaum.ID });
                    }
                    return RedirectToAction("Index", "Uebernahme");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die das Verleihformular mit Räumen, Bestand und Bewegungsarten befüllt.
        /// </summary>
        /// <param name="ausleihe">Das ViewModel des Formulars.</param>
        /// <returns>False, wenn die Person aus keinem Raum verleihen darf.</returns>
        private async Task<bool> AnzeigeSetzenAsync(AusborgenViewModel ausleihe)
        {
            var eigeneRaeume = await _lagerService.RaeumeDerPersonAsync(User.GetPersonID());
            ausleihe.EigeneRaumIDs = eigeneRaeume.Select(r => r.ID).ToList();

            // Verliehen wird aus den eigenen Räumen und als Admin aus jedem Lager.
            ausleihe.VonRaeume.AddRange(eigeneRaeume);
            if (User.IsInRole("Admin"))
            {
                ausleihe.VonRaeume.AddRange((await _lagerService.LagerAsync()).Where(r => !ausleihe.EigeneRaumIDs.Contains(r.ID)));
            }
            if (ausleihe.VonRaeume.Count == 0)
            {
                return false;
            }

            // Ein Raum, aus dem die Person nicht verleihen darf, wird nicht übernommen.
            ausleihe.VonRaum = ausleihe.VonRaeume.FirstOrDefault(r => string.Equals(r.ID, ausleihe.VonRaumID, StringComparison.OrdinalIgnoreCase))
                ?? ausleihe.VonRaeume[0];
            ausleihe.VonRaumID = ausleihe.VonRaum.ID;
            var vonRaumID = ausleihe.VonRaum.ID;

            ausleihe.Bestand = await _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == vonRaumID)
                .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                .ToListAsync();

            // Zielräume brauchen eine verantwortliche Person, die die Übernahme bestätigt. Aus einem fremden Lager geht es nicht in einen eigenen Raum, das geht über "Aus dem Lager holen".
            var ausEigenemRaum = ausleihe.EigeneRaumIDs.Contains(vonRaumID);
            var ausgeschlossen = ausEigenemRaum ? new List<string>() : ausleihe.EigeneRaumIDs;
            ausleihe.Raeume = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.ID != vonRaumID && !ausgeschlossen.Contains(r.ID) && r.PersonID != null)
                .OrderBy(r => r.ID)
                .ToListAsync();

            ausleihe.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", ausleihe.BewegungsartID);
            return true;
        }
    }
}
