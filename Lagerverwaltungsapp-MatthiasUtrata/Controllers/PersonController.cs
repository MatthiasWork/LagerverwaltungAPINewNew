using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Benutzer darf nur ein Admin verwalten, sonst könnte sich jeder zum Admin machen.
[Authorize(Roles = "Admin")]
public class PersonController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly PasswordService _passwordService;
    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext und die Services für Passwörter und Räume übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    /// <param name="passwordService">Der Service für Passwörter.</param>
    /// <param name="lagerService">Der Service für die Raumverantwortung.</param>
    public PersonController(LagerverwaltungContext context, PasswordService passwordService, LagerService lagerService)
    {
        _context = context;
        _passwordService = passwordService;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die zur Adminübersicht weiterleitet, weil die Benutzerliste dort steht.
    /// </summary>
    /// <returns>Die Weiterleitung zur Adminübersicht.</returns>
    // GET: Person
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Admin");
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Person anzeigt.
    /// </summary>
    /// <returns>Das leere Formular.</returns>
    // GET: Person/Create
    public async Task<IActionResult> Create()
    {
        await RollenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Person anlegt, sofern der Benutzername noch frei ist.
    /// Das Passwort wird gehasht gespeichert.
    /// </summary>
    /// <param name="person">Die Person aus dem Formular.</param>
    /// <returns>Die Adminübersicht oder das Formular mit Fehlermeldung.</returns>
    // POST: Person/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Vorname,Nachname,Username,Password,Email,RolleID")] Person person)
    {
        ModelState.Remove(nameof(Person.Rolle));

        // Der Benutzername muss eindeutig sein, weil der Login darüber sucht.
        if (await _context.Person.AnyAsync(p => p.Username == person.Username))
        {
            ModelState.AddModelError(nameof(Person.Username), "Dieser Benutzername ist bereits vergeben.");
        }

        await RolleGueltigPruefenAsync(person);

        if (ModelState.IsValid)
        {
            person.Password = _passwordService.HashPassword(person, person.Password);

            _context.Add(person);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde angelegt.";
            return RedirectToAction("Index", "Admin");
        }
        await RollenSetzenAsync(person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Person anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Person.</param>
    /// <returns>Das Formular oder NotFound, wenn es die Person nicht gibt.</returns>
    // GET: Person/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var person = await _context.Person.FindAsync(id);
        if (person == null)
        {
            return NotFound();
        }
        await RollenSetzenAsync(person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Person speichert.
    /// Ein leeres Passwort lässt das bisherige unverändert.
    /// </summary>
    /// <param name="id">Die ID der Person.</param>
    /// <param name="person">Die Person mit den geänderten Daten.</param>
    /// <returns>Die Adminübersicht oder das Formular mit Fehlermeldung.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Wenn die Person gleichzeitig geändert wurde.</exception>
    // POST: Person/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Vorname,Nachname,Username,Password,Email,RolleID")] Person person)
    {
        ModelState.Remove(nameof(Person.Rolle));

        // Beim Bearbeiten ist das Passwort optional, ein neues muss aber lang genug sein.
        ModelState.Remove(nameof(Person.Password));
        if (!string.IsNullOrEmpty(person.Password) && person.Password.Length < Person.PasswortMindestLaenge)
        {
            ModelState.AddModelError(nameof(Person.Password), $"Das Passwort muss mindestens {Person.PasswortMindestLaenge} Zeichen lang sein.");
        }

        if (id != person.ID)
        {
            return NotFound();
        }

        if (await _context.Person.AnyAsync(p => p.Username == person.Username && p.ID != person.ID))
        {
            ModelState.AddModelError(nameof(Person.Username), "Dieser Benutzername ist bereits vergeben.");
        }

        await RolleGueltigPruefenAsync(person);

        // Der letzte Admin muss Admin bleiben, sonst kann niemand mehr Benutzer verwalten.
        var warAdmin = await _context.Person.Where(p => p.ID == person.ID).Select(p => p.Rolle.Admin).FirstOrDefaultAsync();
        var wirdAdmin = await _context.Rolle.Where(r => r.ID == person.RolleID).Select(r => r.Admin).FirstOrDefaultAsync();
        if (warAdmin && !wirdAdmin && !await GibtEsAndereAdminsAsync(person.ID))
        {
            ModelState.AddModelError(nameof(Person.RolleID), "Der letzte Administrator muss eine Admin-Rolle behalten.");
        }

        if (ModelState.IsValid)
        {
            if (string.IsNullOrEmpty(person.Password))
            {
                // Das bisherige Passwort bleibt erhalten.
                var bisherigerHash = await _context.Person
                    .Where(p => p.ID == person.ID)
                    .Select(p => p.Password)
                    .FirstOrDefaultAsync();
                if (bisherigerHash == null)
                {
                    return NotFound();
                }
                person.Password = bisherigerHash;
            }
            else
            {
                person.Password = _passwordService.HashPassword(person, person.Password);
            }

            try
            {
                _context.Update(person);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(person.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Ein neues Passwort beendet alle Anmeldungen, daher meldet sich der Admin bei einer Änderung seines eigenen Passworts neu an.
            if (person.ID == User.GetPersonID())
            {
                var angemeldet = await _context.Person.AsNoTracking().Include(p => p.Rolle).FirstAsync(p => p.ID == person.ID);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, angemeldet.ErstelleClaimsPrincipal());
            }

            TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde gespeichert.";
            return RedirectToAction("Index", "Admin");
        }
        await RollenSetzenAsync(person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Person anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Person.</param>
    /// <returns>Die Bestätigungsseite oder NotFound, wenn es die Person nicht gibt.</returns>
    // GET: Person/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var person = await _context.Person
            .Include(p => p.Rolle)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (person == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(person);
        return View(person);
    }

    /// <summary>
    /// Methode, die eine Person nach der Bestätigung löscht, sofern nichts dagegen spricht.
    /// </summary>
    /// <param name="id">Die ID der Person.</param>
    /// <returns>Die Adminübersicht, bei einem Hindernis mit Fehlermeldung.</returns>
    // POST: Person/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var person = await _context.Person
            .Include(p => p.Rolle)
            .FirstOrDefaultAsync(p => p.ID == id);
        if (person == null)
        {
            return RedirectToAction("Index", "Admin");
        }

        var hindernis = await LoeschHindernisAsync(person);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction("Index", "Admin");
        }

        _context.Person.Remove(person);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde gelöscht.";
        return RedirectToAction("Index", "Admin");
    }

    /// <summary>
    /// Methode, die prüft, ob es eine Person mit der angegebenen ID gibt.
    /// </summary>
    /// <param name="id">Die ID der Person.</param>
    /// <returns>True, wenn es die Person gibt.</returns>
    private bool PersonExists(int? id)
    {
        return _context.Person.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die die Auswahlliste der Rollen für das Formular füllt.
    /// </summary>
    /// <param name="rolleID">Die ID der vorausgewählten Rolle oder null.</param>
    private async Task RollenSetzenAsync(int? rolleID)
    {
        ViewData["RolleID"] = new SelectList(await _context.Rolle.OrderBy(r => r.Name).ToListAsync(), "ID", "Name", rolleID);
    }

    /// <summary>
    /// Methode, die einen Fehler einträgt, wenn die gewählte Rolle nicht existiert.
    /// Sonst fiele das erst beim Speichern als Datenbankfehler auf.
    /// </summary>
    /// <param name="person">Die Person, die gespeichert werden soll.</param>
    private async Task RolleGueltigPruefenAsync(Person person)
    {
        if (!await _context.Rolle.AnyAsync(r => r.ID == person.RolleID))
        {
            ModelState.AddModelError(nameof(Person.RolleID), "Bitte eine Rolle auswählen.");
        }
    }

    /// <summary>
    /// Methode, die prüft, ob es außer der angegebenen Person noch einen Admin gibt.
    /// </summary>
    /// <param name="personID">Die ID der Person, die nicht mitzählt.</param>
    /// <returns>True, wenn es einen anderen Admin gibt.</returns>
    private async Task<bool> GibtEsAndereAdminsAsync(int personID)
    {
        return await _context.Person.AnyAsync(p => p.ID != personID && p.Rolle.Admin);
    }

    /// <summary>
    /// Methode, die prüft, ob eine Person gelöscht werden darf.
    /// Ihre Rolle muss dafür geladen sein.
    /// </summary>
    /// <param name="person">Die Person, die gelöscht werden soll.</param>
    /// <returns>Der Grund gegen das Löschen oder null, wenn es erlaubt ist.</returns>
    private async Task<string?> LoeschHindernisAsync(Person person)
    {
        if (person.ID == User.GetPersonID())
        {
            return "Du kannst dein eigenes Benutzerkonto nicht löschen.";
        }

        if (person.Rolle.Admin && !await GibtEsAndereAdminsAsync(person.ID))
        {
            return "Der letzte Administrator kann nicht gelöscht werden.";
        }

        // Jeder Raum braucht eine verantwortliche Person.
        var raeume = await _lagerService.RaeumeDerPersonAsync(person.ID);
        if (raeume.Count == 1)
        {
            return $"\"{person.Username}\" ist für den Raum {raeume[0].ID} verantwortlich und kann daher nicht gelöscht werden. Weise den Raum zuerst einer anderen Person zu.";
        }
        if (raeume.Count > 1)
        {
            return $"\"{person.Username}\" ist für die Räume {string.Join(", ", raeume.Select(r => r.ID))} verantwortlich und kann daher nicht gelöscht werden. Weise die Räume zuerst einer anderen Person zu.";
        }

        // Lagerbewegungen brauchen eine Person, damit nachvollziehbar bleibt, wer sie erfasst hat.
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.PersonID == person.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"\"{person.Username}\" hat {anzahlLagerbewegungen} Lagerbewegung(en) erfasst und kann daher nicht gelöscht werden.";
        }

        return null;
    }
}
