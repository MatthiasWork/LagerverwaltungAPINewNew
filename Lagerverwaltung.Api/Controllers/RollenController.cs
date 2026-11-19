using Lagerverwaltung.Api.DTOs;
using Lagerverwaltung.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltung.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public class RollenController : ControllerBase
{
    private readonly BenutzerService _benutzerService;

    public RollenController(BenutzerService benutzerService)
    {
        _benutzerService = benutzerService;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<List<RolleLesenDTO>>> Liste()
    {
        return await _benutzerService.RollenAsync();
    }

    // GET: api/roles/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RolleLesenDTO>> Lesen(int id)
    {
        var rolle = await _benutzerService.RolleLesenAsync(id);
        if (rolle == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nicht gefunden", detail: "Die Rolle wurde nicht gefunden.");
        }
        return Ok(rolle);
    }

    // POST: api/roles
    [HttpPost]
    public async Task<ActionResult<RolleLesenDTO>> Anlegen(RolleDTO dto)
    {
        int id = await _benutzerService.RolleAnlegenAsync(dto);
        var rolle = await _benutzerService.RolleLesenAsync(id);
        return CreatedAtAction(nameof(Lesen), new { id }, rolle);
    }

    // PUT: api/roles/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RolleLesenDTO>> Ändern(int id, RolleDTO dto)
    {
        await _benutzerService.RolleDatenÄndernAsync(id, dto);
        return Ok(await _benutzerService.RolleLesenAsync(id));
    }

    // DELETE: api/roles/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Löschen(int id)
    {
        await _benutzerService.RolleLöschenAsync(id);
        return NoContent();
    }
}
