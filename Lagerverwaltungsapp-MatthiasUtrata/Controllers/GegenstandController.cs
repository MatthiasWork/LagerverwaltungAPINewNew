using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

public class GegenstandController : Controller
{
    private readonly LagerverwaltungContext _context;

    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    /// <param name="lagerService">Der Service für die Zuständigkeiten der Räume.</param>
    public GegenstandController(LagerverwaltungContext context, LagerService lagerService)
    {
        _context = context;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die den Katalog mit allen Gegenständen, ihrem Standort und Status anzeigt.
    /// Die Liste lässt sich filtern.
    /// </summary>
    /// <param name="suche">Der Suchbegriff.</param>
    /// <param name="kategorieID">Die ID der Kategorie.</param>
    /// <param name="herstellerID">Die ID des Herstellers.</param>
    /// <param name="raumID">Die ID des Raums.</param>
    /// <param name="status">Der Status.</param>
    /// <returns>Die Seite mit dem Katalog.</returns>
    // GET: Gegenstand
    public async Task<IActionResult> Index(string? suche, int? kategorieID, int? herstellerID, string? raumID, string? status)
    {
        suche = suche?.Trim();

        // Der Bestand und die noch nicht bestätigten Bewegungen werden gleich mitgeladen.
        var abfrage = _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
            .Include(g => g.Lagerbewegung.Where(l => l.BestaetigtAm == null))
            .AsQueryable();

        if (!string.IsNullOrEmpty(suche))
        {
            abfrage = abfrage.Where(g => g.Name.Contains(suche)
                || (g.Seriennummer != null && g.Seriennummer.Contains(suche))
                || g.Raumbestand.Any(r => r.RaumID.Contains(suche)));
        }

        if (kategorieID != null)
        {
            abfrage = abfrage.Where(g => g.KategorieID == kategorieID);
        }

        if (herstellerID != null)
        {
            abfrage = abfrage.Where(g => g.HerstellerID == herstellerID);
        }

        if (!string.IsNullOrEmpty(raumID))
        {
            abfrage = abfrage.Where(g => g.Raumbestand.Any(r => r.RaumID == raumID));
        }

        var eintraege = (await abfrage.OrderBy(g => g.Name).ThenBy(g => g.Seriennummer).ToListAsync())
            .Select(g => KatalogEintragErstellen(g, raumID))
            .ToList();

        if (!string.IsNullOrEmpty(status))
        {
            eintraege = eintraege.Where(e => e.Status == status).ToList();
        }

        var uebersicht = new GegenstandUebersichtViewModel
        {
            Eintraege = eintraege,
            AnzahlGesamt = await _context.Gegenstand.CountAsync(),
            AnzahlRaeume = await _context.Raum.CountAsync(),
            Suche = suche,
            KategorieID = kategorieID,
            HerstellerID = herstellerID,
            RaumID = raumID,
            Status = status,
            Kategorien = await _context.Kategorie.OrderBy(k => k.Name).ToListAsync(),
            Raeume = await _context.Raum.OrderBy(r => r.ID).Select(r => r.ID).ToListAsync()
        };

        // Der Herstellerfilter erscheint nur über den Link bei den Herstellern, daher werden sie nur dann geladen.
        if (herstellerID != null)
        {
            uebersicht.Hersteller = await _context.Hersteller.OrderBy(h => h.Name).ToListAsync();
        }

        return View(uebersicht);
    }

    /// <summary>
    /// Methode, die für einen Gegenstand Standorte, Stückzahl und Status berechnet.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand mit geladenem Raumbestand und Lagerbewegungen.</param>
    /// <param name="raumID">Die ID des Raums, der bei den Standorten vorne stehen soll.</param>
    /// <returns>Der Katalogeintrag mit Standorten, Stückzahl, Status und Hinweis.</returns>
    private static KatalogEintrag KatalogEintragErstellen(Gegenstand gegenstand, string? raumID = null)
    {
        var eintrag = new KatalogEintrag
        {
            Gegenstand = gegenstand,
            // Bei einem Raumfilter steht dieser Raum vorne, sonst der mit der größten Menge.
            Standorte = gegenstand.Raumbestand.Where(r => r.Menge > 0)
                .OrderByDescending(r => r.RaumID == raumID)
                .ThenByDescending(r => r.Menge)
                .ToList()
        };
        eintrag.Stueck = eintrag.Standorte.Sum(r => r.Menge);

        // Unterwegs ist, was abgebucht, aber weder übernommen noch storniert wurde.
        var offen = gegenstand.Lagerbewegung.Where(l => l.Offen).ToList();
        var unterwegs = offen.Sum(l => l.Menge);
        if (unterwegs > 0)
        {
            var ziele = string.Join(", ", offen.Select(l => l.NachRaumID).Distinct());
            if (gegenstand.Seriennummer != null)
            {
                eintrag.Hinweis = $"unterwegs nach {ziele}";
            }
            else
            {
                eintrag.Hinweis = $"{unterwegs} Stück unterwegs nach {ziele}";
            }
        }

        if (eintrag.Stueck > 0)
        {
            eintrag.Status = GegenstandUebersichtViewModel.Verfuegbar;
        }
        else if (unterwegs > 0)
        {
            eintrag.Status = GegenstandUebersichtViewModel.InAusleihe;
        }
        else
        {
            eintrag.Status = GegenstandUebersichtViewModel.KeinBestand;
        }
        return eintrag;
    }

    /// <summary>
    /// Methode, die die Details eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <returns>Die Seite mit den Details.</returns>
    // GET: Gegenstand/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Bewegungsart)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.VonRaum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.NachRaum).ThenInclude(r => r.Person)
            // Die Raumart wird gebraucht, weil bei einem Lager auch ein Admin freigibt.
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.VonRaum).ThenInclude(r => r.Raumart)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.NachRaum).ThenInclude(r => r.Raumart)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        var details = new GegenstandDetailsViewModel
        {
            Eintrag = KatalogEintragErstellen(gegenstand),
            Unterwegs = gegenstand.Lagerbewegung.Where(l => l.Offen).OrderBy(l => l.ErstelltAm).ToList(),
            EigeneRaumIDs = (await _lagerService.RaeumeDerPersonAsync(User.GetPersonID())).Select(r => r.ID).ToList()
        };

        // Jede Bewegung ergibt einen Verlaufseintrag beim Anlegen und einen weiteren bei Übernahme oder Stornierung.
        foreach (var l in gegenstand.Lagerbewegung)
        {
            var person = l.Person.VollerName;

            // Bei einem Gerät mit Seriennummer ist die Menge immer 1 und wird nicht genannt.
            // Bei einer Korrektur kann die Menge negativ sein, die Richtung steht im Text.
            var menge = "";
            if (gegenstand.Seriennummer == null)
            {
                menge = $"{Math.Abs(l.Menge)} Stück ";
            }

            if (l.IstKorrektur)
            {
                string text;
                if (l.Menge > 0)
                {
                    text = $"{menge}in {l.NachRaumID} eingebucht von {person} (Korrektur)";
                }
                else
                {
                    text = $"{menge}aus {l.NachRaumID} ausgebucht von {person} (Korrektur)";
                }
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang(text) });
                continue;
            }

            // Bei einer stornierten Bewegung steht in BestaetigtAm der Zeitpunkt der Stornierung.
            if (l.Storniert)
            {
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang($"{menge}von {l.VonRaumID} nach {l.NachRaumID} gebucht von {person}") });
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm!.Value, Text = $"Ausleihe nach {l.NachRaumID} storniert, {menge}zurück in {l.VonRaumID}" });
                continue;
            }

            if (l.BestaetigtAm == l.ErstelltAm)
            {
                // Sofort bestätigt ist ein Wareneingang oder eine Umbuchung.
                string text;
                if (l.VonRaumID == l.NachRaumID)
                {
                    text = $"{menge}in {l.NachRaumID} eingebucht von {person}";
                }
                else
                {
                    text = $"{menge}von {l.VonRaumID} nach {l.NachRaumID} umgebucht von {person}";
                }
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang(text) });
                continue;
            }

            details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang($"{menge}von {l.VonRaumID} nach {l.NachRaumID} gebucht von {person} ({l.Bewegungsart.Name})") });
            if (l.BestaetigtAm != null)
            {
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm.Value, Text = GrossAnfang($"{menge}in {l.NachRaumID} übernommen") });
            }
        }
        details.Verlauf = details.Verlauf.OrderByDescending(a => a.Zeitpunkt).ToList();

        return View(details);
    }

    /// <summary>
    /// Methode, die den ersten Buchstaben eines Textes großschreibt.
    /// </summary>
    /// <param name="text">Der Text.</param>
    /// <returns>Der Text mit großem Anfangsbuchstaben.</returns>
    private static string GrossAnfang(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }
        return char.ToUpper(text[0]) + text[1..];
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines Gegenstands anzeigt.
    /// </summary>
    /// <returns>Die Seite mit dem leeren Formular.</returns>
    // GET: Gegenstand/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await AuswahllistenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Gegenstand anlegt.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand mit den Daten aus dem Formular.</param>
    /// <returns>Die Weiterleitung zum Katalog oder bei Fehlern das Formular.</returns>
    // POST: Gegenstand/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        await EingabenPruefenAsync(gegenstand);

        if (ModelState.IsValid)
        {
            _context.Add(gegenstand);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <returns>Die Seite mit dem ausgefüllten Formular.</returns>
    // GET: Gegenstand/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand == null)
        {
            return NotFound();
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Gegenstand speichert.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <param name="gegenstand">Der Gegenstand mit den geänderten Daten aus dem Formular.</param>
    /// <returns>Die Weiterleitung zum Katalog oder bei Fehlern das Formular.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn der Gegenstand gleichzeitig geändert wurde.</exception>
    // POST: Gegenstand/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        if (id != gegenstand.ID)
        {
            return NotFound();
        }

        await EingabenPruefenAsync(gegenstand);

        // Mit Seriennummer wird ein einzelnes Gerät gebucht, ohne Seriennummer eine beliebige Menge.
        // Gibt es schon Bestand oder Lagerbewegungen, ließe sich das nicht mehr ändern, weil die Mengen sonst nicht mehr stimmen.
        var bisherigeSeriennummer = await _context.Gegenstand
            .Where(g => g.ID == gegenstand.ID)
            .Select(g => g.Seriennummer)
            .FirstOrDefaultAsync();
        if ((bisherigeSeriennummer == null) != (gegenstand.Seriennummer == null) && await LoeschHindernisAsync(gegenstand) != null)
        {
            ModelState.AddModelError(nameof(Gegenstand.Seriennummer),
                "Ob der Gegenstand eine Seriennummer hat, kann nur geändert werden, solange er weder Bestand noch Lagerbewegungen hat.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(gegenstand);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GegenstandExists(gegenstand.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <returns>Die Bestätigungsseite.</returns>
    // GET: Gegenstand/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die einen Gegenstand nach der Bestätigung löscht, sofern nichts dagegen spricht.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <returns>Die Weiterleitung zum Katalog.</returns>
    // POST: Gegenstand/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var hindernis = await LoeschHindernisAsync(gegenstand);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Gegenstand.Remove(gegenstand);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob ein Gegenstand mit der ID existiert.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands.</param>
    /// <returns>True, wenn der Gegenstand existiert.</returns>
    private bool GegenstandExists(int? id)
    {
        return _context.Gegenstand.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die die Auswahllisten für Kategorie und Hersteller im Formular setzt.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, dessen Werte vorausgewählt werden, oder null.</param>
    private async Task AuswahllistenSetzenAsync(Gegenstand? gegenstand)
    {
        ViewData["KategorieID"] = new SelectList(await _context.Kategorie.OrderBy(k => k.Name).ToListAsync(), "ID", "Name", gegenstand?.KategorieID);
        ViewData["HerstellerID"] = new SelectList(await _context.Hersteller.OrderBy(h => h.Name).ToListAsync(), "ID", "Name", gegenstand?.HerstellerID);
    }

    /// <summary>
    /// Methode, die Kategorie, Hersteller und Seriennummer prüft und Fehler im ModelState einträgt.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, der gespeichert werden soll.</param>
    private async Task EingabenPruefenAsync(Gegenstand gegenstand)
    {
        if (string.IsNullOrWhiteSpace(gegenstand.Seriennummer))
        {
            gegenstand.Seriennummer = null;
        }
        else
        {
            gegenstand.Seriennummer = gegenstand.Seriennummer.Trim();
        }

        if (!await _context.Kategorie.AnyAsync(k => k.ID == gegenstand.KategorieID))
        {
            ModelState.AddModelError(nameof(Gegenstand.KategorieID), "Bitte eine Kategorie auswählen.");
        }

        if (!await _context.Hersteller.AnyAsync(h => h.ID == gegenstand.HerstellerID))
        {
            ModelState.AddModelError(nameof(Gegenstand.HerstellerID), "Bitte einen Hersteller auswählen.");
        }

        // Ein Gegenstand mit Seriennummer ist genau ein Gerät, daher muss die Seriennummer eindeutig sein.
        if (gegenstand.Seriennummer != null
            && await _context.Gegenstand.AnyAsync(g => g.Seriennummer == gegenstand.Seriennummer && g.ID != gegenstand.ID))
        {
            ModelState.AddModelError(nameof(Gegenstand.Seriennummer), "Diese Seriennummer ist bereits vergeben.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob ein Gegenstand gelöscht werden darf.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, der gelöscht werden soll.</param>
    /// <returns>Der Grund, warum nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Gegenstand gegenstand)
    {
        var raeume = await _context.Raumbestand
            .Where(r => r.GegenstandID == gegenstand.ID)
            .Select(r => r.RaumID)
            .OrderBy(r => r)
            .ToListAsync();
        if (raeume.Any())
        {
            return $"\"{gegenstand.Name}\" ist noch im Bestand (Raum {string.Join(", ", raeume)}) und kann daher nicht gelöscht werden.";
        }

        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.GegenstandID == gegenstand.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"\"{gegenstand.Name}\" kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }
}
