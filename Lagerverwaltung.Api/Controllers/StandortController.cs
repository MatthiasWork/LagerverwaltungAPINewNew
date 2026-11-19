using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/locations")]
public class StandorteController : ControllerBase
{
    private readonly StandortService _standortService;
    private readonly BuchungService _buchungService;

    public StandorteController(StandortService standortService, BuchungService buchungService)
    {
        _standortService = standortService;
        _buchungService = buchungService;
    }

    // GET: api/locations?suche=&raumartId=&personId=
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<RaumByIDLesenDTO>>> Liste(string? suche, int? raumartId, int? personId)
    {
        return await _standortService.ListeAsync(suche, raumartId, personId);
    }

    // GET: api/locations/mine
    [HttpGet("mine")]
    public async Task<ActionResult<List<RaumByIDLesenDTO>>> MeineRäume()
    {
        return await _standortService.MeineRäumeAsync(User.GetPersonID());
    }

    // GET: api/locations/HL
    [HttpGet("{id}")]
    public async Task<ActionResult<RaumByIDLesenDTO>> Lesen(string id)
    {
        return await _standortService.LesenAsync(User.GetPersonID(), id);
    }

    // POST: api/locations
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RaumByIDLesenDTO>> Anlegen(RaumAnlegenDTO raum)
    {
        string id = await _standortService.AnlegenAsync(raum);
        var standort = await _standortService.LesenAsync(User.GetPersonID(), id);
        return CreatedAtAction(nameof(Lesen), new { id }, standort);
    }

    // PUT: api/locations/HL
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RaumByIDLesenDTO>> Ändern(string id, RaumÄndernDTO raum)
    {
        await _standortService.ÄndernAsync(id, raum);
        return await _standortService.LesenAsync(User.GetPersonID(), id);
    }

    // DELETE: api/locations/101
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Löschen(string id)
    {
        await _standortService.LöschenAsync(id);
        return NoContent();
    }

    // PUT: api/locations/101/responsible
    [HttpPut("{id}/responsible")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RaumByIDLesenDTO>> VerantwortlichSetzen(string id, VerantwortlichDTO raum)
    {
        return await _standortService.VerantwortlicheSetzenAsync(User.GetPersonID(), id, raum.PersonId);
    }

    // GET: api/locations/101/inventory?name=&kategorieId=&seriennummer=
    [HttpGet("{id}/inventory")]
    public async Task<ActionResult<List<RaumbestandByRaumLesenDTO>>> Inventar(string id, string? name, int? kategorieId, string? seriennummer)
    {
        return await _standortService.InventarAsync(User.GetPersonID(), id, name, kategorieId, seriennummer);
    }

    // GET: api/locations/101/movements?status=&gegenstandId=
    [HttpGet("{id}/movements")]
    public async Task<ActionResult<List<LagerbewegungLesenDTO>>> Bewegungen(string id, string? status, int? gegenstandId)
    {
        return await _buchungService.BewegungenAsync(User.GetPersonID(), id, gegenstandId, status, null, null, null);
    }
}
