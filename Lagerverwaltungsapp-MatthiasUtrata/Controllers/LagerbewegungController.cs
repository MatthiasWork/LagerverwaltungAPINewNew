using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Nur ein Admin darf Lagerbewegungen ansehen. Sie entstehen nur über Buchungen und werden nie geändert oder gelöscht, sonst passt der Raumbestand nicht mehr zur Historie.
[Authorize(Roles = "Admin")]
public class LagerbewegungController : Controller
{
    private readonly LagerverwaltungContext _context;

    public LagerbewegungController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die die Historie aller Lagerbewegungen, die neueste zuerst, seitenweise anzeigt.
    /// Die Liste kann gefiltert werden.
    /// </summary>
    /// <param name="suche">Der Suchbegriff für Gegenstand, Seriennummer oder Person.</param>
    /// <param name="raumID">Die ID des Raums, aus dem oder in den gebucht wurde.</param>
    /// <param name="bewegungsartID">Die ID der Bewegungsart.</param>
    /// <param name="status">Der Status der Lagerbewegung.</param>
    /// <param name="von">Der erste Tag des Zeitraums.</param>
    /// <param name="bis">Der letzte Tag des Zeitraums.</param>
    /// <param name="seite">Die Nummer der Seite, ab 1.</param>
    /// <returns>Die Seite mit der Historie.</returns>
    // GET: Lagerbewegung
    public async Task<IActionResult> Index(string? suche, string? raumID, int? bewegungsartID, string? status, DateTime? von, DateTime? bis, int seite = 1)
    {
        suche = suche?.Trim();

        var abfrage = _context.Lagerbewegung.AsQueryable();

        if (!string.IsNullOrEmpty(suche))
        {
            abfrage = abfrage.Where(l => l.Gegenstand.Name.Contains(suche)
                || (l.Gegenstand.Seriennummer != null && l.Gegenstand.Seriennummer.Contains(suche))
                || (l.Person.Vorname + " " + l.Person.Nachname).Contains(suche)
                || l.Person.Username.Contains(suche));
        }

        if (!string.IsNullOrEmpty(raumID))
        {
            abfrage = abfrage.Where(l => l.VonRaumID == raumID || l.NachRaumID == raumID);
        }

        if (bewegungsartID != null)
        {
            abfrage = abfrage.Where(l => l.BewegungsartID == bewegungsartID);
        }

        if (status == HistorieViewModel.FreigabeOffen)
        {
            abfrage = abfrage.Where(l => l.BestaetigtAm == null);
        }
        else if (status == HistorieViewModel.Storniert)
        {
            abfrage = abfrage.Where(l => l.BestaetigtAm != null && l.Bewegungsart.Name == Bewegungsart.Storniert);
        }
        else if (status == HistorieViewModel.Bestaetigt)
        {
            abfrage = abfrage.Where(l => l.BestaetigtAm != null && l.Bewegungsart.Name != Bewegungsart.Storniert);
        }

        // Die Spalte ErstelltAm kennt erst Tage ab 1753, ein früheres Datum ergäbe einen Datenbankfehler.
        var fruehesterTag = new DateTime(1753, 1, 1);

        if (von != null && von.Value.Date > fruehesterTag)
        {
            var anfang = von.Value.Date;
            abfrage = abfrage.Where(l => l.ErstelltAm >= anfang);
        }

        if (bis != null && bis.Value.Date < fruehesterTag)
        {
            abfrage = abfrage.Where(l => false);
        }
        // Am letzten möglichen Tag gibt es keinen nächsten Tag, dann schließt "bis" nichts aus.
        else if (bis != null && bis.Value.Date < DateTime.MaxValue.Date)
        {
            // Bis einschließlich: alles vor dem Beginn des nächsten Tages.
            var ende = bis.Value.Date.AddDays(1);
            abfrage = abfrage.Where(l => l.ErstelltAm < ende);
        }

        // Eine Seite zu weit zeigt die letzte Seite.
        var anzahlGefiltert = await abfrage.CountAsync();
        var anzahlSeiten = Math.Max(1, (int)Math.Ceiling(anzahlGefiltert / (double)HistorieViewModel.EintraegeJeSeite));
        seite = Math.Clamp(seite, 1, anzahlSeiten);

        var historie = new HistorieViewModel
        {
            Lagerbewegungen = await abfrage
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .Include(l => l.Person)
                .Include(l => l.VonRaum).ThenInclude(r => r.Person)
                .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                .OrderByDescending(l => l.ErstelltAm)
                .ThenByDescending(l => l.ID)
                .Skip((seite - 1) * HistorieViewModel.EintraegeJeSeite)
                .Take(HistorieViewModel.EintraegeJeSeite)
                .ToListAsync(),
            AnzahlGesamt = await _context.Lagerbewegung.CountAsync(),
            AnzahlGefiltert = anzahlGefiltert,
            Seite = seite,
            AnzahlSeiten = anzahlSeiten,
            Suche = suche,
            RaumID = raumID,
            BewegungsartID = bewegungsartID,
            Status = status,
            Von = von,
            Bis = bis,
            Raeume = await _context.Raum.OrderBy(r => r.ID).Select(r => r.ID).ToListAsync(),
            Bewegungsarten = await _context.Bewegungsart.OrderBy(b => b.Name).ToListAsync()
        };

        return View(historie);
    }

    /// <summary>
    /// Methode, die alle Lagerbewegungen chronologisch sortiert als CSV-Datei liefert.
    /// </summary>
    /// <returns>Die CSV-Datei zum Herunterladen.</returns>
    // GET: Lagerbewegung/Export
    public async Task<IActionResult> Export()
    {
        var lagerbewegungen = await _context.Lagerbewegung
            .Include(l => l.Bewegungsart)
            .Include(l => l.Gegenstand)
            .Include(l => l.Person)
            .OrderBy(l => l.ErstelltAm)
            .ThenBy(l => l.ID)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Gebucht am;Abgeschlossen am;Status;Bewegungsart;Gegenstand;Seriennummer;Menge;Von Raum;Nach Raum;Gebucht von");

        foreach (var l in lagerbewegungen)
        {
            // Zuerst auf offen prüfen, denn eine stornierte Lagerbewegung hat in BestaetigtAm den Zeitpunkt der Stornierung.
            string status;
            if (l.Offen)
            {
                status = "Offen";
            }
            else if (l.Storniert)
            {
                status = "Storniert";
            }
            else
            {
                status = "Bestätigt";
            }

            var abgeschlossen = "";
            if (l.BestaetigtAm != null)
            {
                abgeschlossen = l.BestaetigtAm.Value.ToString("dd.MM.yyyy HH:mm");
            }

            // Bei einer Korrektur ist die Menge die Bestandsänderung und kann negativ sein.
            csv.AppendLine(string.Join(";",
                l.ErstelltAm.ToString("dd.MM.yyyy HH:mm"),
                abgeschlossen,
                status,
                CsvWert(l.Bewegungsart.Name),
                CsvWert(l.Gegenstand.Name),
                CsvWert(l.Gegenstand.Seriennummer),
                l.Menge,
                CsvWert(l.VonRaumID),
                CsvWert(l.NachRaumID),
                CsvWert(l.Person.VollerName)));
        }

        // Mit UTF-8-Kennung am Dateianfang, sonst zeigt Excel Umlaute falsch an.
        var inhalt = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(inhalt, "text/csv; charset=utf-8", $"Lagerbewegungen_{DateTime.Now:yyyy-MM-dd}.csv");
    }

    /// <summary>
    /// Methode, die einen Text für eine Zelle der CSV-Datei aufbereitet. Sie schützt vor verschobenen Spalten und vor Formeln in Excel.
    /// </summary>
    /// <param name="wert">Der Text, darf leer sein.</param>
    /// <returns>Der Text für die CSV-Datei.</returns>
    private static string CsvWert(string? wert)
    {
        if (string.IsNullOrEmpty(wert))
        {
            return "";
        }

        if ("=+-@".Contains(wert[0]))
        {
            wert = "'" + wert;
        }

        if (wert.IndexOfAny([';', '"', '\r', '\n']) >= 0)
        {
            wert = "\"" + wert.Replace("\"", "\"\"") + "\"";
        }
        return wert;
    }
}
