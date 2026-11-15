using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Raumarten darf nur ein Admin verwalten.
[Authorize(Roles = "Admin")]
public class RaumartController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public RaumartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Raumarten anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Raumarten.</returns>
    // GET: Raumart
    public async Task<IActionResult> Index()
    {
        // Die Räume werden für die Anzeige mitgeladen.
        return View(await _context.Raumart.Include(r => r.Raum).OrderBy(r => r.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <returns>Die Detailseite oder NotFound, wenn es die Raumart nicht gibt.</returns>
    // GET: Raumart/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart
            .Include(r => r.Raum)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raumart == null)
        {
            return NotFound();
        }

        return View(raumart);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Raumart anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Raumart/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Raumart anlegt, sofern es noch keine Raumart mit diesem Namen gibt.
    /// </summary>
    /// <param name="raumart">Die Raumart aus dem Formular.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Raumart/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Raumart raumart)
    {
        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Labor" neben dem Namen ohne Leerzeichen.
        raumart.Name = raumart.Name?.Trim()!;
        await NameEindeutigPruefenAsync(raumart);

        if (ModelState.IsValid)
        {
            _context.Add(raumart);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Raumart \"{raumart.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <returns>Das Formular oder NotFound, wenn es die Raumart nicht gibt.</returns>
    // GET: Raumart/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart.FindAsync(id);
        if (raumart == null)
        {
            return NotFound();
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Raumart speichert, sofern der Name noch nicht vergeben ist.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <param name="raumart">Die Raumart mit den geänderten Daten.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn die Raumart gleichzeitig geändert wurde.</exception>
    // POST: Raumart/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Raumart raumart)
    {
        if (id != raumart.ID)
        {
            return NotFound();
        }

        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Labor" neben dem Namen ohne Leerzeichen.
        raumart.Name = raumart.Name?.Trim()!;
        await NameEindeutigPruefenAsync(raumart);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(raumart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaumartExists(raumart.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Raumart \"{raumart.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es die Raumart nicht gibt.</returns>
    // GET: Raumart/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raumart == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(raumart);
        return View(raumart);
    }

    /// <summary>
    /// Methode, die eine Raumart nach der Bestätigung löscht, sofern ihr keine Räume mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <returns>Die Übersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Raumart/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var raumart = await _context.Raumart.FindAsync(id);
        if (raumart != null)
        {
            // Erneut prüfen, weil der POST auch ohne Bestätigungsseite kommen kann.
            var hindernis = await LoeschHindernisAsync(raumart);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Raumart.Remove(raumart);
            TempData["Meldung"] = $"Raumart \"{raumart.Name}\" wurde gelöscht.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob es eine Raumart mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID der Raumart.</param>
    /// <returns>True, wenn es die Raumart gibt.</returns>
    private bool RaumartExists(int? id)
    {
        return _context.Raumart.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die einen Fehler einträgt, wenn der Name der Raumart schon vergeben ist.
    /// </summary>
    /// <param name="raumart">Die Raumart, die gespeichert werden soll.</param>
    private async Task NameEindeutigPruefenAsync(Raumart raumart)
    {
        if (await _context.Raumart.AnyAsync(r => r.Name == raumart.Name && r.ID != raumart.ID))
        {
            ModelState.AddModelError(nameof(Raumart.Name), "Diese Raumart gibt es bereits.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob eine Raumart gelöscht werden darf. Jeder Raum braucht eine Raumart.
    /// </summary>
    /// <param name="raumart">Die Raumart, die gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Raumart raumart)
    {
        var raeume = await _context.Raum.Where(r => r.RaumartID == raumart.ID).OrderBy(r => r.ID).Select(r => r.ID).ToListAsync();
        if (raeume.Count > 0)
        {
            string zugeordnet;
            if (raeume.Count == 1)
            {
                zugeordnet = $"ist noch der Raum {raeume[0]} zugeordnet. Dieser muss";
            }
            else
            {
                zugeordnet = $"sind noch {raeume.Count} Räume zugeordnet ({string.Join(", ", raeume)}). Diese müssen";
            }
            return $"Der Raumart \"{raumart.Name}\" {zugeordnet} zuerst eine andere Raumart bekommen oder gelöscht werden.";
        }

        return null;
    }
}
