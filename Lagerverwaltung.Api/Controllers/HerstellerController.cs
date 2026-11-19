using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Models;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/manufacturers")]
public class HerstellerController : ControllerBase
{
    private readonly HerstellerService _herstellerService;

    public HerstellerController(HerstellerService herstellerService)
    {
        _herstellerService = herstellerService;
    }

    // GET: api/manufacturers?name=
    [HttpGet]
    public async Task<ActionResult<List<Hersteller>>> Liste(string? name)
    {
        return await _herstellerService.ListeAsync(name);
    }

    // GET: api/manufacturers/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Hersteller>> Lesen(int id)
    {
        var hersteller = await _herstellerService.LesenAsync(id);
        if (hersteller == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Der Hersteller wurde nicht gefunden.");
        }
        return Ok(hersteller);
    }

    // POST: api/manufacturers
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Hersteller>> Anlegen(HerstellerDTO hersteller)
    {
        var neuHersteller = await _herstellerService.AnlegenAsync(hersteller);
        return CreatedAtAction(nameof(Lesen), new { id = neuHersteller.ID }, neuHersteller);
    }

    // PUT: api/manufacturers/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Hersteller>> Ändern(int id, HerstellerDTO hersteller)
    {
        return await _herstellerService.ÄndernAsync(id, hersteller);
    }

    // DELETE: api/manufacturers/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _herstellerService.LöschenAsync(id);
        return NoContent();
    }
}
