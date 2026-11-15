using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Ein Admin wählt einen Raum, sieht dessen Bestand und kann ihn korrigieren.
[Authorize(Roles = "Admin")]
public class RaumbestandController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor, der den Datenbankkontext und den Lagerservice übernimmt.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    /// <param name="lagerService">Der Service für die Korrekturbuchungen.</param>
    public RaumbestandController(LagerverwaltungContext context, LagerService lagerService)
    {
        _context = context;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die die Räume zur Auswahl und den Bestand des gewählten Raums zum Korrigieren anzeigt.
    /// </summary>
    /// <param name="id">Die ID des gewählten Raums; ohne ID nur die Auswahl.</param>
    /// <returns>Die Seite mit dem Raumbestand.</returns>
    // GET: Raumbestand, Raumbestand/Index/A101
    public async Task<IActionResult> Index(string? id)
    {
        var korrektur = new KorrekturViewModel { RaumID = id };
        await AnzeigeSetzenAsync(korrektur);
        return View(korrektur);
    }

    /// <summary>
    /// Methode, die die geänderten Mengen des gewählten Raums als Korrektur bucht.
    /// </summary>
    /// <param name="korrektur">Die Eingaben aus dem Formular.</param>
    /// <returns>Die Weiterleitung zum Raumbestand oder bei einem Fehler das Formular.</returns>
    // POST: Raumbestand
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind("RaumID,Mengen,Bisher,NeuGegenstandID,NeuMenge")] KorrekturViewModel korrektur)
    {
        var personID = User.GetPersonID();
        if (personID == null)
        {
            return Forbid();
        }

        if (korrektur.NeuGegenstandID != null && korrektur.NeuMenge == null)
        {
            ModelState.AddModelError(nameof(KorrekturViewModel.NeuMenge), "Bitte eine Menge für den neuen Gegenstand eingeben.");
        }

        if (ModelState.IsValid)
        {
            var positionen = new Dictionary<int, (int Bisher, int Neu)>();
            foreach (var (gegenstandID, neu) in korrektur.Mengen)
            {
                var bisher = korrektur.Bisher.GetValueOrDefault(gegenstandID);
                if (neu != null && neu != bisher)
                {
                    positionen[gegenstandID] = (bisher, neu.Value);
                }
            }

            // Ein hinzugefügter Gegenstand war bisher nicht im Raum; mit Menge 0 ändert sich nichts.
            if (korrektur.NeuGegenstandID != null && korrektur.NeuMenge != 0)
            {
                positionen[korrektur.NeuGegenstandID.Value] = (0, korrektur.NeuMenge!.Value);
            }

            var fehler = await _lagerService.KorrigierenAsync(korrektur.RaumID!, positionen, personID.Value);
            if (fehler == null)
            {
                if (positionen.Count == 1)
                {
                    TempData["Meldung"] = $"Bestand von Raum {korrektur.RaumID} korrigiert: 1 Gegenstand geändert.";
                }
                else
                {
                    TempData["Meldung"] = $"Bestand von Raum {korrektur.RaumID} korrigiert: {positionen.Count} Gegenstände geändert.";
                }
                return RedirectToAction(nameof(Index), new { id = korrektur.RaumID });
            }
            ModelState.AddModelError(string.Empty, fehler);
        }

        await AnzeigeSetzenAsync(korrektur);
        return View(korrektur);
    }

    /// <summary>
    /// Methode, die die Seite mit Räumen, Bestand und hinzufügbaren Gegenständen befüllt.
    /// </summary>
    /// <param name="korrektur">Das ViewModel der Seite.</param>
    private async Task AnzeigeSetzenAsync(KorrekturViewModel korrektur)
    {
        korrektur.Raeume = await _context.Raum
            .Include(r => r.Raumart)
            .OrderBy(r => r.ID)
            .ToListAsync();

        korrektur.Raum = korrektur.Raeume.FirstOrDefault(r => string.Equals(r.ID, korrektur.RaumID, StringComparison.OrdinalIgnoreCase));
        korrektur.RaumID = korrektur.Raum?.ID;
        if (korrektur.Raum == null)
        {
            return;
        }

        var raumID = korrektur.Raum.ID;
        korrektur.Bestand = await _context.Raumbestand
            .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
            .Where(r => r.RaumID == raumID)
            .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
            .ToListAsync();

        // Ein Gerät mit Seriennummer gibt es nur einmal; liegt es schon in einem Raum oder ist es unterwegs, kann es nicht hinzukommen.
        korrektur.Hinzufuegbar = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Where(g => !g.Raumbestand.Any(r => r.RaumID == raumID))
            .Where(g => g.Seriennummer == null
                || (!g.Raumbestand.Any() && !g.Lagerbewegung.Any(l => l.BestaetigtAm == null)))
            .OrderBy(g => g.Name).ThenBy(g => g.Seriennummer)
            .ToListAsync();
    }
}
