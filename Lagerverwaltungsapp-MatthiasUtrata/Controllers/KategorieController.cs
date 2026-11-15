using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Kategorien darf nur ein Admin verwalten.
[Authorize(Roles = "Admin")]
public class KategorieController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public KategorieController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Kategorien mit der Anzahl ihrer Gegenstände anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Kategorien.</returns>
    // GET: Kategorie
    public async Task<IActionResult> Index()
    {
        return View(await _context.Kategorie.Include(k => k.Gegenstand).OrderBy(k => k.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <returns>Die Detailseite oder NotFound, wenn es die Kategorie nicht gibt.</returns>
    // GET: Kategorie/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie
            .Include(k => k.Gegenstand)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (kategorie == null)
        {
            return NotFound();
        }

        return View(kategorie);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Kategorie anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Kategorie/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Kategorie anlegt, sofern es noch keine Kategorie mit diesem Namen gibt.
    /// </summary>
    /// <param name="kategorie">Die Kategorie aus dem Formular.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Kategorie/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Kategorie kategorie)
    {
        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Kabel" neben dem Namen ohne Leerzeichen.
        kategorie.Name = kategorie.Name?.Trim()!;
        await NameEindeutigPruefenAsync(kategorie);

        if (ModelState.IsValid)
        {
            _context.Add(kategorie);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <returns>Das Formular oder NotFound, wenn es die Kategorie nicht gibt.</returns>
    // GET: Kategorie/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie.FindAsync(id);
        if (kategorie == null)
        {
            return NotFound();
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Kategorie speichert, sofern der Name noch nicht vergeben ist.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <param name="kategorie">Die Kategorie mit den geänderten Daten.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn die Kategorie gleichzeitig geändert wurde.</exception>
    // POST: Kategorie/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Kategorie kategorie)
    {
        if (id != kategorie.ID)
        {
            return NotFound();
        }

        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Kabel" neben dem Namen ohne Leerzeichen.
        kategorie.Name = kategorie.Name?.Trim()!;
        await NameEindeutigPruefenAsync(kategorie);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(kategorie);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KategorieExists(kategorie.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es die Kategorie nicht gibt.</returns>
    // GET: Kategorie/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie
            .FirstOrDefaultAsync(m => m.ID == id);
        if (kategorie == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(kategorie);
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die eine Kategorie nach der Bestätigung löscht, sofern ihr keine Gegenstände mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <returns>Die Übersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Kategorie/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var kategorie = await _context.Kategorie.FindAsync(id);
        if (kategorie == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Erneut prüfen, weil der POST auch ohne Bestätigungsseite kommen kann.
        var hindernis = await LoeschHindernisAsync(kategorie);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Kategorie.Remove(kategorie);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob es eine Kategorie mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie.</param>
    /// <returns>True, wenn es die Kategorie gibt.</returns>
    private bool KategorieExists(int? id)
    {
        return _context.Kategorie.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die einen Fehler einträgt, wenn der Name der Kategorie schon vergeben ist.
    /// </summary>
    /// <param name="kategorie">Die Kategorie, die gespeichert werden soll.</param>
    private async Task NameEindeutigPruefenAsync(Kategorie kategorie)
    {
        if (await _context.Kategorie.AnyAsync(k => k.Name == kategorie.Name && k.ID != kategorie.ID))
        {
            ModelState.AddModelError(nameof(Kategorie.Name), "Diese Kategorie gibt es bereits.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob eine Kategorie gelöscht werden darf.
    /// </summary>
    /// <param name="kategorie">Die Kategorie, die gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Kategorie kategorie)
    {
        var anzahlGegenstaende = await _context.Gegenstand.CountAsync(g => g.KategorieID == kategorie.ID);
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
            return $"Der Kategorie \"{kategorie.Name}\" {gegenstaende} zugeordnet. "
                + "Diese müssen zuerst einer anderen Kategorie zugeordnet oder gelöscht werden.";
        }

        return null;
    }
}
