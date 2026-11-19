using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class KategorienController : ControllerBase
{
    private readonly KategorieService _kategorieService;

    public KategorienController(KategorieService kategorieService)
    {
        _kategorieService = kategorieService;
    }

    // GET: api/categories?name=
    [HttpGet]
    public async Task<ActionResult<List<Kategorie>>> Liste(string? name)
    {
        return await _kategorieService.ListeAsync(name);
    }

    // GET: api/categories/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Kategorie>> Lesen(int id)
    {
        var kategorie = await _kategorieService.LesenAsync(id);
        if (kategorie == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Die Kategorie wurde nicht gefunden.");
        }
        return Ok(kategorie);
    }

    // POST: api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Kategorie>> Anlegen(KategorieDTO dto)
    {
        var kategorie = await _kategorieService.AnlegenAsync(dto);
        return CreatedAtAction(nameof(Lesen), new { id = kategorie.ID }, kategorie);
    }

    // PUT: api/categories/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Kategorie>> Ändern(int id, KategorieDTO dto)
    {
        return await _kategorieService.ÄndernAsync(id, dto);
    }

    // DELETE: api/categories/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _kategorieService.LöschenAsync(id);
        return NoContent();
    }
}
