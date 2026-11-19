using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/movement-types")]
[Authorize(Roles = "Admin")]
public class BewegungsartenController : ControllerBase
{
    private readonly BewegungsartService _bewegungsartService;

    public BewegungsartenController(BewegungsartService bewegungsartService)
    {
        _bewegungsartService = bewegungsartService;
    }

    // GET: api/movement-types?name=
    [HttpGet]
    public async Task<ActionResult<List<Bewegungsart>>> Liste(string? name)
    {
        return await _bewegungsartService.ListeAsync(name);
    }

    // GET: api/movement-types/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Bewegungsart>> Lesen(int id)
    {
        var bewegungsart = await _bewegungsartService.LesenAsync(id);
        if (bewegungsart == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Die Bewegungsart wurde nicht gefunden.");
        }
        return Ok(bewegungsart);
    }

    // POST: api/movement-types
    [HttpPost]
    public async Task<ActionResult<Bewegungsart>> Anlegen(BewegungsartDTO dto)
    {
        var bewegungsart = await _bewegungsartService.AnlegenAsync(dto);
        return CreatedAtAction(nameof(Lesen), new { id = bewegungsart.ID }, bewegungsart);
    }

    // PUT: api/movement-types/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Bewegungsart>> Ändern(int id, BewegungsartDTO dto)
    {
        return await _bewegungsartService.ÄndernAsync(id, dto);
    }

    // DELETE: api/movement-types/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _bewegungsartService.LöschenAsync(id);
        return NoContent();
    }
}
