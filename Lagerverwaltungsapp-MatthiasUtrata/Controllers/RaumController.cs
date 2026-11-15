using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Räume verwalten darf nur ein Admin.
[Authorize(Roles = "Admin")]
public class RaumController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public RaumController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Räume mit Raumart und verantwortlicher Person anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Räumen.</returns>
    // GET: Raum
    public async Task<IActionResult> Index()
    {
        var raeume = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .OrderBy(r => r.ID)
            .ToListAsync();
        return View(raeume);
    }

    /// <summary>
    /// Methode, die die Details eines Raums mit den Gegenständen anzeigt, die gerade im Raum liegen.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <returns>Die Seite mit den Details.</returns>
    // GET: Raum/Details/A101
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Es wird nur der aktuelle Bestand geladen, denn was unterwegs ist, steht noch nicht im Nach-Raum.
        var raum = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .Include(r => r.Raumbestand.Where(b => b.Menge > 0)).ThenInclude(b => b.Gegenstand).ThenInclude(g => g.Kategorie)
            .Include(r => r.Raumbestand).ThenInclude(b => b.Gegenstand).ThenInclude(g => g.Hersteller)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        return View(raum);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines Raums anzeigt.
    /// </summary>
    /// <returns>Die Seite mit dem leeren Formular.</returns>
    // GET: Raum/Create
    public async Task<IActionResult> Create()
    {
        await AuswahllistenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Raum anlegt, sofern die Raumnummer noch nicht vergeben ist.
    /// </summary>
    /// <param name="raum">Der Raum mit den Daten aus dem Formular.</param>
    /// <returns>Die Weiterleitung zur Raumliste oder bei Fehlern das Formular.</returns>
    // POST: Raum/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,PersonID,RaumartID")] Raum raum)
    {
        ModelState.Remove(nameof(Raum.Raumart));

        // Die Raumnummer steht in der Adresszeile, daher muss sie eindeutig sein und darf keine Zeichen wie / oder Leerzeichen enthalten.
        if (raum.ID != null)
        {
            raum.ID = raum.ID.Trim();
            if (!Regex.IsMatch(raum.ID, "^[A-Za-z0-9._-]+$"))
            {
                ModelState.AddModelError(nameof(Raum.ID), "Die Raumnummer darf nur Buchstaben, Ziffern, Punkte, Binde- und Unterstriche enthalten.");
            }
            // Der Browser kürzt Adressen mit ".." weg, so ein Raum ließe sich nie mehr öffnen, bearbeiten oder löschen.
            else if (raum.ID.All(zeichen => zeichen == '.'))
            {
                ModelState.AddModelError(nameof(Raum.ID), "Die Raumnummer darf nicht nur aus Punkten bestehen.");
            }
            else if (await _context.Raum.AnyAsync(r => r.ID == raum.ID))
            {
                ModelState.AddModelError(nameof(Raum.ID), $"Den Raum {raum.ID} gibt es bereits.");
            }
        }

        await EingabenPruefenAsync(raum);

        if (ModelState.IsValid)
        {
            _context.Add(raum);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Raum {raum.ID} wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Raums anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <returns>Die Seite mit dem ausgefüllten Formular.</returns>
    // GET: Raum/Edit/A101
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum.FindAsync(id);
        if (raum == null)
        {
            return NotFound();
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Raum speichert.
    /// Eine neue verantwortliche Person ist sofort auch für schon offene Lagerbewegungen zuständig.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <param name="raum">Der Raum mit den geänderten Daten aus dem Formular.</param>
    /// <returns>Die Weiterleitung zur Raumliste oder bei Fehlern das Formular.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn der Raum gleichzeitig geändert wurde.</exception>
    // POST: Raum/Edit/A101
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? id, [Bind("ID,PersonID,RaumartID")] Raum raum)
    {
        ModelState.Remove(nameof(Raum.Raumart));

        if (id != raum.ID)
        {
            return NotFound();
        }

        await EingabenPruefenAsync(raum);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(raum);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaumExists(raum.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Raum {raum.ID} wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Raums anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <returns>Die Bestätigungsseite.</returns>
    // GET: Raum/Delete/A101
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die einen Raum nach der Bestätigung löscht, sofern er leer ist und in keiner Lagerbewegung vorkommt.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <returns>Die Weiterleitung zur Raumliste.</returns>
    // POST: Raum/Delete/A101
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? id)
    {
        var raum = await _context.Raum.FindAsync(id);
        if (raum == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var hindernis = await LoeschHindernisAsync(raum);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Raum.Remove(raum);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Raum {raum.ID} wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob ein Raum mit der ID existiert.
    /// </summary>
    /// <param name="id">Die ID des Raums.</param>
    /// <returns>True, wenn der Raum existiert.</returns>
    private bool RaumExists(string? id)
    {
        return _context.Raum.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die die Auswahllisten für Raumart und verantwortliche Person im Formular setzt.
    /// Es werden alle Personen angeboten, denn eine Person kann für mehrere Räume verantwortlich sein.
    /// </summary>
    /// <param name="raum">Der Raum, dessen Werte vorausgewählt werden, oder null bei einem neuen Raum.</param>
    private async Task AuswahllistenSetzenAsync(Raum? raum)
    {
        ViewData["RaumartID"] = new SelectList(await _context.Raumart.OrderBy(r => r.Name).ToListAsync(), "ID", "Name", raum?.RaumartID);

        var personen = await _context.Person
            .OrderBy(p => p.Nachname).ThenBy(p => p.Vorname)
            .Select(p => new { p.ID, Name = p.Vorname + " " + p.Nachname + " (" + p.Username + ")" })
            .ToListAsync();
        ViewData["PersonID"] = new SelectList(personen, "ID", "Name", raum?.PersonID);
    }

    /// <summary>
    /// Methode, die Raumart und verantwortliche Person prüft und Fehler im ModelState einträgt.
    /// </summary>
    /// <param name="raum">Der Raum, der gespeichert werden soll.</param>
    private async Task EingabenPruefenAsync(Raum raum)
    {
        if (!await _context.Raumart.AnyAsync(r => r.ID == raum.RaumartID))
        {
            ModelState.AddModelError(nameof(Raum.RaumartID), "Bitte eine Raumart auswählen.");
        }

        await PersonWechselnPruefenAsync(raum);

        if (raum.PersonID == null)
        {
            return;
        }

        if (!await _context.Person.AnyAsync(p => p.ID == raum.PersonID))
        {
            ModelState.AddModelError(nameof(Raum.PersonID), "Diese Person gibt es nicht.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob die verantwortliche Person eines Raums entzogen oder gewechselt werden darf.
    /// Das geht nicht, solange eine offene Lagerbewegung den Raum betrifft, sonst könnte niemand sie freigeben
    /// oder die neue Person könnte sich eine Bewegung selbst freigeben.
    /// </summary>
    /// <param name="raum">Der Raum, der gespeichert werden soll.</param>
    private async Task PersonWechselnPruefenAsync(Raum raum)
    {
        var bisherigePerson = await _context.Raum
            .Where(r => r.ID == raum.ID)
            .Select(r => r.PersonID)
            .FirstOrDefaultAsync();
        if (bisherigePerson == null || bisherigePerson == raum.PersonID)
        {
            return;
        }

        var anzahlOffen = await _context.Lagerbewegung
            .Where(Lagerbewegung.IstOffen)
            .CountAsync(l => l.VonRaumID == raum.ID || l.NachRaumID == raum.ID);
        if (anzahlOffen > 0)
        {
            ModelState.AddModelError(nameof(Raum.PersonID),
                $"Die verantwortliche Person von Raum {raum.ID} kann nicht entzogen oder gewechselt werden, solange {anzahlOffen} offene Lagerbewegung(en) ihn betreffen. "
                + "Sie müssen zuerst bestätigt, abgelehnt oder zurückgezogen werden.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob ein Raum gelöscht werden darf.
    /// </summary>
    /// <param name="raum">Der Raum, der gelöscht werden soll.</param>
    /// <returns>Der Grund, warum nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Raum raum)
    {
        // Liegt noch etwas im Raum, ginge dieser Bestand beim Löschen verloren.
        if (await _context.Raumbestand.AnyAsync(r => r.RaumID == raum.ID))
        {
            return $"Im Raum {raum.ID} ist noch Bestand vorhanden. Der Raum kann erst gelöscht werden, wenn er leer ist.";
        }

        // Lagerbewegungen werden nie gelöscht, sonst wäre die Historie unvollständig.
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.VonRaumID == raum.ID || l.NachRaumID == raum.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"Der Raum {raum.ID} kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }
}
