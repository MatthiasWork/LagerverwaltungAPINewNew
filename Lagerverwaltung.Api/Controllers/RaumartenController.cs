using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/location-types")]
[Authorize(Roles = "Admin")]
public class RaumartenController : ControllerBase
{
    private readonly StandortService _standortService;

    public RaumartenController(StandortService standortService)
    {
        _standortService = standortService;
    }

    // GET: api/location-types
    [HttpGet]
    public async Task<ActionResult<List<RaumartLesenDTO>>> Liste()
    {
        return await _standortService.RaumartenAsync();
    }

    // GET: api/location-types/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RaumartLesenDTO>> Lesen(int id)
    {
        var raumart = await _standortService.RaumartLesenAsync(id);
        if (raumart == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Die Raumart wurde nicht gefunden.");
        }
        return Ok(raumart);
    }

    // POST: api/location-types
    [HttpPost]
    public async Task<ActionResult<RaumartLesenDTO>> Anlegen(RaumartDTO dto)
    {
        int id = await _standortService.RaumartAnlegenAsync(dto);
        var raumart = await _standortService.RaumartLesenAsync(id);
        return CreatedAtAction(nameof(Lesen), new { id }, raumart);
    }

    // PUT: api/location-types/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RaumartLesenDTO>> Ändern(int id, RaumartDTO dto)
    {
        await _standortService.RaumartÄndernAsync(id, dto);
        return Ok(await _standortService.RaumartLesenAsync(id));
    }

    // DELETE: api/location-types/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _standortService.RaumartLöschenAsync(id);
        return NoContent();
    }
}
