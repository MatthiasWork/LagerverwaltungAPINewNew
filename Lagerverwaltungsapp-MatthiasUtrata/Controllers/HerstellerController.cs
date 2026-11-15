using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Hersteller darf nur ein Admin verwalten.
[Authorize(Roles = "Admin")]
public class HerstellerController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public HerstellerController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Hersteller mit der Anzahl ihrer Gegenstände anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Herstellern.</returns>
    // GET: Hersteller
    public async Task<IActionResult> Index()
    {
        return View(await _context.Hersteller.Include(h => h.Gegenstand).OrderBy(h => h.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <returns>Die Detailseite oder NotFound, wenn es den Hersteller nicht gibt.</returns>
    // GET: Hersteller/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller
            .Include(h => h.Gegenstand)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (hersteller == null)
        {
            return NotFound();
        }

        return View(hersteller);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines neuen Herstellers anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Hersteller/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Hersteller anlegt, sofern es noch keinen Hersteller mit diesem Namen gibt.
    /// </summary>
    /// <param name="hersteller">Der Hersteller aus dem Formular.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Hersteller/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Hersteller hersteller)
    {
        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Dell" neben dem Namen ohne Leerzeichen.
        hersteller.Name = hersteller.Name?.Trim()!;
        await NameEindeutigPruefenAsync(hersteller);

        if (ModelState.IsValid)
        {
            _context.Add(hersteller);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <returns>Das Formular oder NotFound, wenn es den Hersteller nicht gibt.</returns>
    // GET: Hersteller/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller.FindAsync(id);
        if (hersteller == null)
        {
            return NotFound();
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Hersteller speichert, sofern der Name noch nicht vergeben ist.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <param name="hersteller">Der Hersteller mit den geänderten Daten.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn der Hersteller gleichzeitig geändert wurde.</exception>
    // POST: Hersteller/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Hersteller hersteller)
    {
        if (id != hersteller.ID)
        {
            return NotFound();
        }

        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Dell" neben dem Namen ohne Leerzeichen.
        hersteller.Name = hersteller.Name?.Trim()!;
        await NameEindeutigPruefenAsync(hersteller);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(hersteller);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HerstellerExists(hersteller.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es den Hersteller nicht gibt.</returns>
    // GET: Hersteller/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller
            .FirstOrDefaultAsync(m => m.ID == id);
        if (hersteller == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(hersteller);
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die einen Hersteller nach der Bestätigung löscht, sofern ihm keine Gegenstände mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <returns>Die Übersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Hersteller/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var hersteller = await _context.Hersteller.FindAsync(id);
        if (hersteller == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var hindernis = await LoeschHindernisAsync(hersteller);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Hersteller.Remove(hersteller);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob es einen Hersteller mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers.</param>
    /// <returns>True, wenn es den Hersteller gibt.</returns>
    private bool HerstellerExists(int? id)
    {
        return _context.Hersteller.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die einen Fehler einträgt, wenn der Name des Herstellers schon vergeben ist.
    /// </summary>
    /// <param name="hersteller">Der Hersteller, der gespeichert werden soll.</param>
    private async Task NameEindeutigPruefenAsync(Hersteller hersteller)
    {
        if (await _context.Hersteller.AnyAsync(h => h.Name == hersteller.Name && h.ID != hersteller.ID))
        {
            ModelState.AddModelError(nameof(Hersteller.Name), "Diesen Hersteller gibt es bereits.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob ein Hersteller gelöscht werden darf.
    /// </summary>
    /// <param name="hersteller">Der Hersteller, der gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Hersteller hersteller)
    {
        var anzahlGegenstaende = await _context.Gegenstand.CountAsync(g => g.HerstellerID == hersteller.ID);
        if (anzahlGegenstaende > 0)
        {
            string gegenstaende;
            if (anzahlGegenstaende == 1)
            {
                gegenstaende = "ist noch 1 Gegenstand";
            }
            else
            {
                gegenstaende = $"sind noch {anzahlGegenstaende} Gegenstände";
            }
            return $"Dem Hersteller \"{hersteller.Name}\" {gegenstaende} zugeordnet. "
                + "Diese müssen zuerst einem anderen Hersteller zugeordnet oder gelöscht werden.";
        }

        return null;
    }
}
