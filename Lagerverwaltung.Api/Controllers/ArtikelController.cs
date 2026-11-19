using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArtikelController : ControllerBase
{
    private readonly ArtikelService _artikelService;
    private readonly BuchungService _buchungService;

    public ArtikelController(ArtikelService artikelService, BuchungService buchungService)
    {
        _artikelService = artikelService;
        _buchungService = buchungService;
    }

    // GET: api/articles?name=&kategorieId=&herstellerId=&seriennummer=&mitSeriennummer=&raumId=&personId=
    [HttpGet]
    public async Task<ActionResult<List<ArtikelLesenDTO>>> Suchen(string? name, int? kategorieId, int? herstellerId,
        string? seriennummer, bool? mitSeriennummer, string? raumId, int? personId)
    {
        return await _artikelService.SuchenAsync(User.GetPersonID(), name, kategorieId, herstellerId,
            seriennummer, mitSeriennummer, raumId, personId);
    }

    // GET: api/articles/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArtikelDetailsDTO>> Lesen(int id)
    {
        int angemeldetID = User.GetPersonID();
        var artikel = await _artikelService.LesenAsync(angemeldetID, id);
        if (artikel == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Artikel wurde nicht gefunden.");
        }

        return new ArtikelDetailsDTO
        {
            Artikel = artikel,
            Bestaende = await _artikelService.BeständeAsync(angemeldetID, id)
        };
    }

    // GET: api/articles/5/stock
    [HttpGet("{id:int}/stock")]
    public async Task<ActionResult<List<RaumbestandByArtikelLesenDTO>>> Bestände(int id)
    {
        int angemeldetID = User.GetPersonID();
        if (await _artikelService.LesenAsync(angemeldetID, id) == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Artikel wurde nicht gefunden.");
        }

        return await _artikelService.BeständeAsync(angemeldetID, id);
    }

    // GET: api/articles/5/movements?status=
    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Bewegungen(int id, string? status)
    {
        int angemeldetID = User.GetPersonID();
        if (await _artikelService.LesenAsync(angemeldetID, id) == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Artikel wurde nicht gefunden.");
        }

        return await _buchungService.SichtbareBewegungenAsync(angemeldetID, User.IsInRole("Admin"), id, status);
    }

    // POST: api/articles
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ArtikelLesenDTO>> Anlegen(ArtikelDTO artikel)
    {
        int id = await _artikelService.AnlegenAsync(artikel);
        var neuArtikel = await _artikelService.LesenAsync(User.GetPersonID(), id);
        return CreatedAtAction(nameof(Lesen), new { id }, neuArtikel);
    }

    // PUT: api/articles/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ArtikelLesenDTO>> Ändern(int id, ArtikelDTO artikel)
    {
        await _artikelService.ÄndernAsync(id, artikel);
        return Ok(await _artikelService.LesenAsync(User.GetPersonID(), id));
    }

    // DELETE: api/articles/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _artikelService.LöschenAsync(id);
        return NoContent();
    }
}
