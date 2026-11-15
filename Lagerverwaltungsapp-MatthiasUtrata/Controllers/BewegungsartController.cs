using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Bewegungsarten darf nur ein Admin verwalten.
[Authorize(Roles = "Admin")]
public class BewegungsartController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public BewegungsartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Bewegungsarten anzeigt.
    /// </summary>
    /// <returns>Die Seite mit allen Bewegungsarten.</returns>
    // GET: Bewegungsart
    public async Task<IActionResult> Index()
    {
        return View(await _context.Bewegungsart.ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <returns>Die Detailseite oder NotFound, wenn es die Bewegungsart nicht gibt.</returns>
    // GET: Bewegungsart/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (bewegungsart == null)
        {
            return NotFound();
        }

        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Bewegungsart anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Bewegungsart/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Bewegungsart anlegt, sofern es noch keine mit diesem Namen gibt.
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart aus dem Formular.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Bewegungsart/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Bewegungsart bewegungsart)
    {
        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Ausgabe" neben dem Namen ohne Leerzeichen.
        bewegungsart.Name = bewegungsart.Name?.Trim()!;
        FesteNichtNeuVergeben(bewegungsart);
        await NameEindeutigPruefenAsync(bewegungsart);

        if (ModelState.IsValid)
        {
            _context.Add(bewegungsart);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Bewegungsart \"{bewegungsart.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <returns>Das Formular oder NotFound, wenn es die Bewegungsart nicht gibt.</returns>
    // GET: Bewegungsart/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart.FindAsync(id);
        if (bewegungsart == null)
        {
            return NotFound();
        }
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Bewegungsart speichert.
    /// Die festen Bewegungsarten können nicht umbenannt werden.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <param name="bewegungsart">Die Bewegungsart mit den geänderten Daten.</param>
    /// <returns>Die Übersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn die Bewegungsart gleichzeitig geändert wurde.</exception>
    // POST: Bewegungsart/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Bewegungsart bewegungsart)
    {
        if (id != bewegungsart.ID)
        {
            return NotFound();
        }

        var bisherigerName = await _context.Bewegungsart.Where(b => b.ID == bewegungsart.ID).Select(b => b.Name).FirstOrDefaultAsync();
        if (bisherigerName == null)
        {
            return NotFound();
        }

        // Leerzeichen am Rand werden entfernt, sonst gäbe es " Ausgabe" neben dem Namen ohne Leerzeichen.
        bewegungsart.Name = bewegungsart.Name?.Trim()!;

        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bisherigerName);
        if (nurVergebenFuer != null)
        {
            // Der LagerService sucht sie über den Namen, daher dürfen sie nicht umbenannt werden.
            if (bewegungsart.Name != bisherigerName)
            {
                ModelState.AddModelError(nameof(Bewegungsart.Name),
                    $"Die Bewegungsart \"{bisherigerName}\" bekommen {nurVergebenFuer}, daher kann sie nicht umbenannt werden.");
            }
        }
        else
        {
            FesteNichtNeuVergeben(bewegungsart);
            await NameEindeutigPruefenAsync(bewegungsart);
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(bewegungsart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BewegungsartExists(bewegungsart.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Bewegungsart \"{bewegungsart.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es die Bewegungsart nicht gibt.</returns>
    // GET: Bewegungsart/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (bewegungsart == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(bewegungsart);
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die eine Bewegungsart nach der Bestätigung löscht, sofern nichts dagegen spricht.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <returns>Die Übersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Bewegungsart/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var bewegungsart = await _context.Bewegungsart.FindAsync(id);
        if (bewegungsart != null)
        {
            // Erneut prüfen, weil der POST auch ohne Bestätigungsseite kommen kann.
            var hindernis = await LoeschHindernisAsync(bewegungsart);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Bewegungsart.Remove(bewegungsart);
            TempData["Meldung"] = $"Bewegungsart \"{bewegungsart.Name}\" wurde gelöscht.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob es eine Bewegungsart mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart.</param>
    /// <returns>True, wenn es die Bewegungsart gibt.</returns>
    private bool BewegungsartExists(int? id)
    {
        return _context.Bewegungsart.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die prüft, ob eine Bewegungsart gelöscht werden darf.
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart, die gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Bewegungsart bewegungsart)
    {
        // Der LagerService vergibt diese Bewegungsarten selbst und sucht sie über den Namen.
        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
        if (nurVergebenFuer != null)
        {
            return $"Die Bewegungsart \"{bewegungsart.Name}\" bekommen {nurVergebenFuer}, daher kann sie nicht gelöscht werden.";
        }

        // Lagerbewegungen werden nie gelöscht, damit die Historie vollständig bleibt.
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.BewegungsartID == bewegungsart.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"Die Bewegungsart \"{bewegungsart.Name}\" kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }

    /// <summary>
    /// Methode, die verhindert, dass eine neue Bewegungsart wie eine feste heißt.
    /// Fest sind "Storniert", "Korrektur" und "Rueckgabe".
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart aus dem Formular.</param>
    private void FesteNichtNeuVergeben(Bewegungsart bewegungsart)
    {
        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
        if (nurVergebenFuer != null)
        {
            ModelState.AddModelError(nameof(Bewegungsart.Name),
                $"Die Bewegungsart \"{bewegungsart.Name!.Trim()}\" gibt es schon. Sie bekommen nur {nurVergebenFuer}.");
        }
    }

    /// <summary>
    /// Methode, die einen Fehler einträgt, wenn der Name der Bewegungsart schon vergeben ist.
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart, die gespeichert werden soll.</param>
    private async Task NameEindeutigPruefenAsync(Bewegungsart bewegungsart)
    {
        // Feste Namen meldet schon FesteNichtNeuVergeben.
        if (Bewegungsart.NurVergebenFuer(bewegungsart.Name) != null)
        {
            return;
        }

        if (await _context.Bewegungsart.AnyAsync(b => b.Name == bewegungsart.Name && b.ID != bewegungsart.ID))
        {
            ModelState.AddModelError(nameof(Bewegungsart.Name), "Diese Bewegungsart gibt es bereits.");
        }
    }
}
