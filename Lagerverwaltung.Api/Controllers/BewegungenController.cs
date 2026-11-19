using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/movements")]
[Authorize(Roles = "Admin")]
public class BewegungenController : ControllerBase
{
    private readonly BuchungService _buchungService;

    public BewegungenController(BuchungService buchungService)
    {
        _buchungService = buchungService;
    }

    // GET: api/movements?raumId=&gegenstandId=&status=&bewegungsart=&von=&bis=
    [HttpGet]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Liste(string? raumId, int? gegenstandId, string? status,
        string? bewegungsart, DateTime? von, DateTime? bis)
    {
        return await _buchungService.BewegungenAsync(User.GetPersonID(), raumId, gegenstandId, status, bewegungsart, von, bis);
    }

    // GET: api/movements/export?raumId=&gegenstandId=&status=&bewegungsart=&von=&bis=
    [HttpGet("export")]
    public async Task<IActionResult> Export(string? raumId, int? gegenstandId, string? status,
        string? bewegungsart, DateTime? von, DateTime? bis)
    {
        var bewegungen = await _buchungService.BewegungenAsync(User.GetPersonID(), raumId, gegenstandId, status, bewegungsart, von, bis);

        var csv = new StringBuilder();
        csv.AppendLine("Gebucht am;Abgeschlossen am;Status;Bewegungsart;Gegenstand;Seriennummer;Menge;Von Raum;Nach Raum;Gebucht von");
        // In der Datei steht die älteste Bewegung zuerst
        foreach (var b in bewegungen.OrderBy(b => b.ErstelltAm).ThenBy(b => b.ID))
        {
            // Bei einer Korrektur ist die Menge die Bestandsänderung und kann negativ sein
            csv.AppendLine(string.Join(";",
                b.ErstelltAm.ToString("dd.MM.yyyy HH:mm"),
                b.BestaetigtAm?.ToString("dd.MM.yyyy HH:mm") ?? "",
                CsvWert(b.Status),
                CsvWert(b.Bewegungsart),
                CsvWert(b.Gegenstand),
                CsvWert(b.Seriennummer),
                b.Menge,
                CsvWert(b.VonRaumID),
                CsvWert(b.NachRaumID),
                CsvWert(b.Person)));
        }

        // Mit UTF-8-Kennung am Dateianfang, sonst zeigt Excel Umlaute falsch an
        byte[] inhalt = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(inhalt, "text/csv; charset=utf-8", $"Lagerbewegungen_{DateTime.Now:yyyy-MM-dd}.csv");
    }

    /// <summary>
    /// Methode, die einen Text so umwandelt, dass er in einer CSV-Datei mit Strichpunkt als Trennzeichen in einer Zelle bleibt
    /// </summary>
    /// <param name="wert">Der Text der Zelle</param>
    /// <returns>Gibt den Text für die CSV-Datei zurück</returns>
    private static string CsvWert(string? wert)
    {
        if (string.IsNullOrEmpty(wert))
        {
            return "";
        }

        // Excel würde einen Text mit diesen Zeichen am Anfang als Formel ausführen
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
