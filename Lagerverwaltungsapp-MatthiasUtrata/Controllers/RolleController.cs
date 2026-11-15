using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Rollen darf nur ein Admin verwalten.
[Authorize(Roles = "Admin")]
public class RolleController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public RolleController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Rollen mit der Anzahl ihrer Benutzer anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Rollen.</returns>
    // GET: Rolle
    public async Task<IActionResult> Index()
    {
        // Die Benutzer werden für die Anzahl mitgeladen.
        return View(await _context.Rolle.Include(r => r.Person).OrderBy(r => r.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Rolle anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <returns>Die Detailseite oder NotFound, wenn es die Rolle nicht gibt.</returns>
    // GET: Rolle/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle
            .Include(r => r.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (rolle == null)
        {
            return NotFound();
        }

        return View(rolle);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Rolle anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Rolle/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Rolle anlegt.
    /// </summary>
    /// <param name="rolle">Die Rolle aus dem Formular.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Rolle/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Admin")] Rolle rolle)
    {
        if (ModelState.IsValid)
        {
            _context.Add(rolle);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Rolle \"{rolle.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(rolle);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Rolle anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <returns>Das Formular oder NotFound, wenn es die Rolle nicht gibt.</returns>
    // GET: Rolle/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle == null)
        {
            return NotFound();
        }
        return View(rolle);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Rolle speichert.
    /// Die letzte Rolle mit Admin-Rechten und Benutzern darf diese Rechte nicht verlieren.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <param name="rolle">Die Rolle mit den geänderten Daten.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn die Rolle gleichzeitig geändert wurde.</exception>
    // POST: Rolle/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Admin")] Rolle rolle)
    {
        if (id != rolle.ID)
        {
            return NotFound();
        }

        var warAdmin = await _context.Rolle.Where(r => r.ID == rolle.ID).Select(r => r.Admin).FirstOrDefaultAsync();
        if (warAdmin && !rolle.Admin
            && await _context.Person.AnyAsync(p => p.RolleID == rolle.ID)
            && !await _context.Person.AnyAsync(p => p.RolleID != rolle.ID && p.Rolle.Admin))
        {
            ModelState.AddModelError(nameof(Rolle.Admin),
                "Diese Rolle ist die einzige mit Admin-Rechten, der Benutzer zugeordnet sind. Ohne sie könnte niemand mehr Benutzer verwalten.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rolle);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolleExists(rolle.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Rolle \"{rolle.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(rolle);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Rolle anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es die Rolle nicht gibt.</returns>
    // GET: Rolle/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle
            .FirstOrDefaultAsync(m => m.ID == id);
        if (rolle == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(rolle);
        return View(rolle);
    }

    /// <summary>
    /// Methode, die eine Rolle nach der Bestätigung löscht, sofern ihr keine Personen mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <returns>Die Übersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Rolle/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle != null)
        {
            // Erneut prüfen, weil der POST auch ohne Bestätigungsseite kommen kann.
            var hindernis = await LoeschHindernisAsync(rolle);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Rolle.Remove(rolle);
            TempData["Meldung"] = $"Rolle \"{rolle.Name}\" wurde gelöscht.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob es eine Rolle mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID der Rolle.</param>
    /// <returns>True, wenn es die Rolle gibt.</returns>
    private bool RolleExists(int? id)
    {
        return _context.Rolle.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die prüft, ob eine Rolle gelöscht werden darf. Jede Person braucht eine Rolle.
    /// </summary>
    /// <param name="rolle">Die Rolle, die gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Rolle rolle)
    {
        var anzahlPersonen = await _context.Person.CountAsync(p => p.RolleID == rolle.ID);
        if (anzahlPersonen > 0)
        {
            string personen;
            if (anzahlPersonen == 1)
            {
                personen = "ist noch 1 Person zugeordnet. Diese muss";
            }
            else
            {
                personen = $"sind noch {anzahlPersonen} Personen zugeordnet. Diese müssen";
            }
            return $"Der Rolle \"{rolle.Name}\" {personen} zuerst eine andere Rolle bekommen.";
        }

        return null;
    }
}
