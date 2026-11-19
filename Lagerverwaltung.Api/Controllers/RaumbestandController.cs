using Lagerverwaltung.Api.Authentication;
using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

// Bestandskorrektur nach einer Inventur. Jede Änderung bucht die Prozedur als Lagerbewegung "Korrektur"
// Gelesen wird der Bestand eines Raums über GET api/locations/{id}/inventory
[ApiController]
[Route("api/locations/{raumId}/inventory")]
[Authorize(Roles = "Admin")]
public class RaumbestandController : ControllerBase
{
    private readonly RaumbestandService _raumbestandService;

    public RaumbestandController(RaumbestandService raumbestandService)
    {
        _raumbestandService = raumbestandService;
    }

    // POST: api/locations/101/inventory
    [HttpPost]
    public async Task<ActionResult<RaumbestandByRaumLesenDTO>> Anlegen(string raumId, RaumbestandAnlegenDTO dto)
    {
        var bestand = await _raumbestandService.AnlegenAsync(User.GetPersonID(), dto.GegenstandId, raumId, dto.Menge);
        return StatusCode(StatusCodes.Status201Created, bestand);
    }

    // PUT: api/locations/101/inventory/5
    [HttpPut("{gegenstandId:int}")]
    public async Task<ActionResult<RaumbestandByRaumLesenDTO>> Ändern(string raumId, int gegenstandId, RaumbestandÄndernDTO dto)
    {
        return await _raumbestandService.ÄndernAsync(User.GetPersonID(), gegenstandId, raumId, dto.Menge);
    }

    // DELETE: api/locations/101/inventory/5
    [HttpDelete("{gegenstandId:int}")]
    public async Task<IActionResult> Löschen(string raumId, int gegenstandId)
    {
        await _raumbestandService.LöschenAsync(User.GetPersonID(), gegenstandId, raumId);
        return NoContent();
    }
}
