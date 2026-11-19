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
    private readonly RaumartService _raumartService;

    public RaumartenController(RaumartService raumartService)
    {
        _raumartService = raumartService;
    }

    // GET: api/location-types
    [HttpGet]
    public async Task<ActionResult<List<RaumartLesenDTO>>> Liste()
    {
        return await _raumartService.ListeAsync();
    }

    // GET: api/location-types/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RaumartLesenDTO>> Lesen(int id)
    {
        var raumart = await _raumartService.LesenAsync(id);
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
        int id = await _raumartService.AnlegenAsync(dto);
        var raumart = await _raumartService.LesenAsync(id);
        return CreatedAtAction(nameof(Lesen), new { id }, raumart);
    }

    // PUT: api/location-types/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RaumartLesenDTO>> Ändern(int id, RaumartDTO dto)
    {
        await _raumartService.ÄndernAsync(id, dto);
        return Ok(await _raumartService.LesenAsync(id));
    }

    // DELETE: api/location-types/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _raumartService.LöschenAsync(id);
        return NoContent();
    }
}
